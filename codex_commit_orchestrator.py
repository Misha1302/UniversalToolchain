#!/usr/bin/env python3
"""
Codex commit orchestrator (single-branch mode, resumable).

Features
--------
1. Loads commit specs from a local folder or a GitHub folder URL.
2. Sorts specs by the first natural number found in each filename.
3. Creates a single dedicated work branch from the base branch.
4. Runs Codex commit-by-commit on that same branch.
5. Creates one local git commit per spec.
6. Pushes the branch once at the end.
7. Optionally creates one final PR, but never merges it automatically.
8. Saves orchestration state after each important step so the run can be resumed.
9. Supports selecting the starting commit index when beginning a new run.

Artifacts and state
-------------------
- Artifacts live under `.automation_artifacts/` inside the repository.
- State is stored in `.automation_artifacts/single_branch_state.json` by default.

Resume model
------------
- If a previous run exists, the script can resume it.
- The script remembers:
  * repository path,
  * base branch,
  * work branch,
  * commit spec ordering,
  * completed commit indices,
  * whether the branch was pushed,
  * whether the final PR was created.
- If a run stops because of internet loss or any other failure, rerun the script and choose resume.
"""

from __future__ import annotations

import argparse
import base64
import json
import pathlib
import random
import re
import shlex
import string
import subprocess
import sys
import textwrap
import time
from dataclasses import asdict, dataclass
from datetime import datetime
from typing import Iterable
from urllib.parse import quote


DEFAULT_BASE_BRANCH = "auto"
DEFAULT_STATE_FILENAME = "single_branch_state.json"
RULE_FILES = [
    "PROJECT_RULES.md",
    "AGENTS.md",
    "CONTRIBUTING.md",
    "readme.md",
]
TRANSIENT_GH_MARKERS = [
    "TLS handshake timeout",
    "i/o timeout",
    "connection timeout",
    "connection reset by peer",
    "EOF",
    "HTTP 502",
    "HTTP 503",
    "HTTP 504",
    "net/http:",
]


@dataclass
class CommitSpec:
    index: int
    name: str
    title: str
    body: str
    source: str
    suggested_commit_message: str


class CommandError(RuntimeError):
    pass


class StepError(RuntimeError):
    pass


class ResumeRequiredError(StepError):
    pass


class DirtyWorktreeError(StepError):
    pass


@dataclass
class StateSnapshot:
    version: int
    repo_path: str
    spec_source: str
    base_branch: str
    work_branch: str
    created_at: str
    updated_at: str
    status: str
    start_from: int
    ordered_spec_names: list[str]
    ordered_spec_indices: list[int]
    completed_indices: list[int]
    current_index: int | None
    pushed: bool
    final_pr_url: str | None
    final_review_path: str | None
    final_pr_created: bool
    skip_final_pr: bool
    last_error: str | None


# ---------- Printing helpers ----------


def print_header(message: str) -> None:
    line = "=" * len(message)
    print(f"\n{line}\n{message}\n{line}")



def print_step(message: str) -> None:
    print(f"[+] {message}")



def print_warn(message: str) -> None:
    print(f"[!] {message}")



def print_info(message: str) -> None:
    print(f"    {message}")


# ---------- Generic helpers ----------


def ask(prompt: str, default: str | None = None) -> str:
    suffix = f" [{default}]" if default else ""
    while True:
        value = input(f"{prompt}{suffix}: ").strip()
        if value:
            return value
        if default is not None:
            return default
        print("Please enter a value.")



def ask_yes_no(prompt: str, default: bool = True) -> bool:
    suffix = "[Y/n]" if default else "[y/N]"
    while True:
        value = input(f"{prompt} {suffix}: ").strip().lower()
        if not value:
            return default
        if value in {"y", "yes"}:
            return True
        if value in {"n", "no"}:
            return False
        print("Please answer yes or no.")



def run(
    args: list[str],
    *,
    cwd: pathlib.Path | None = None,
    check: bool = True,
    capture_output: bool = True,
    text: bool = True,
    input_text: str | None = None,
) -> subprocess.CompletedProcess:
    process = subprocess.run(
        args,
        cwd=str(cwd) if cwd else None,
        text=text,
        capture_output=capture_output,
        input=input_text,
    )

    if check and process.returncode != 0:
        command = " ".join(shlex.quote(part) for part in args)
        raise CommandError(
            f"Command failed ({process.returncode}): {command}\n"
            f"STDOUT:\n{process.stdout}\nSTDERR:\n{process.stderr}"
        )

    return process



def run_with_retry(
    args: list[str],
    *,
    cwd: pathlib.Path | None = None,
    retries: int = 6,
    base_sleep_seconds: float = 2.0,
) -> subprocess.CompletedProcess:
    last_error: CommandError | None = None
    for attempt in range(1, retries + 1):
        try:
            return run(args, cwd=cwd, check=True)
        except CommandError as exc:
            last_error = exc
            text_exc = str(exc)
            if not any(marker in text_exc for marker in TRANSIENT_GH_MARKERS):
                raise
            if attempt == retries:
                raise
            sleep_seconds = base_sleep_seconds * attempt
            print_warn(
                f"Transient network/API failure. Retrying in {sleep_seconds:.1f}s "
                f"({attempt}/{retries})"
            )
            time.sleep(sleep_seconds)
    assert last_error is not None
    raise last_error



def ensure_tool_exists(name: str, version_args: list[str] | None = None) -> None:
    version_args = version_args or ["--version"]
    try:
        result = run([name, *version_args], check=True)
        version_line = (result.stdout or result.stderr or "").strip().splitlines()
        if version_line:
            print_step(f"{name} detected: {version_line[0]}")
        else:
            print_step(f"{name} detected")
    except Exception as exc:
        raise StepError(f"Required tool `{name}` is not available: {exc}") from exc



def normalize_repo_path(path_text: str) -> pathlib.Path:
    path = pathlib.Path(path_text).expanduser().resolve()
    if not path.exists() or not path.is_dir():
        raise StepError(f"Repository path does not exist or is not a directory: {path}")
    if not (path / ".git").exists():
        raise StepError(f"Directory is not a git repository: {path}")
    return path



def get_artifacts_dir(repo_path: pathlib.Path) -> pathlib.Path:
    artifacts_dir = repo_path / ".automation_artifacts"
    artifacts_dir.mkdir(parents=True, exist_ok=True)
    return artifacts_dir



def get_default_state_path(repo_path: pathlib.Path) -> pathlib.Path:
    return get_artifacts_dir(repo_path) / DEFAULT_STATE_FILENAME



def safe_branch_slug(text: str) -> str:
    value = text.lower()
    value = re.sub(r"[^a-z0-9]+", "-", value)
    value = value.strip("-")
    return value[:48] or "task"



def random_suffix(length: int = 6) -> str:
    alphabet = string.ascii_lowercase + string.digits
    return "".join(random.choice(alphabet) for _ in range(length))



def extract_first_natural_number(name: str) -> int:
    match = re.search(r"(\d+)", name)
    if not match:
        raise StepError(f"Filename does not contain a natural number: {name}")
    return int(match.group(1))



def sort_commit_spec_paths(paths: Iterable[pathlib.Path]) -> list[pathlib.Path]:
    return sorted(paths, key=lambda path: (extract_first_natural_number(path.name), path.name.lower()))


# ---------- Auth and repo checks ----------


def ensure_git_clean(repo_path: pathlib.Path) -> None:
    result = run(["git", "status", "--porcelain"], cwd=repo_path)
    if result.stdout.strip():
        raise DirtyWorktreeError(
            "Repository has uncommitted changes. Either commit/reset them, or resume only after the worktree is clean."
        )



def ensure_codex_authenticated(repo_path: pathlib.Path) -> None:
    print_step("Checking Codex authentication")
    try:
        result = run(["codex", "login", "status"], cwd=repo_path, check=False)
    except FileNotFoundError as exc:
        raise StepError("Codex CLI is not installed.") from exc

    combined_output = (result.stdout or "") + (result.stderr or "")
    if result.returncode == 0 and "Logged in" in combined_output:
        print_step("Codex authentication looks OK")
        return

    print_warn("Codex CLI is not authenticated yet, or the quick auth check failed.")
    print_info("The script will pause so you can finish ChatGPT sign-in for Codex.")
    print_info("Run `codex login` in another terminal if needed.")
    input("Press Enter after Codex login is completed...")

    retry = run(["codex", "login", "status"], cwd=repo_path, check=False)
    combined_retry_output = (retry.stdout or "") + (retry.stderr or "")
    if retry.returncode != 0 or "Logged in" not in combined_retry_output:
        raise StepError(
            "Codex authentication still failed after retry.\n"
            f"STDOUT:\n{retry.stdout}\nSTDERR:\n{retry.stderr}"
        )

    print_step("Codex authentication verified after manual sign-in")



def ensure_gh_authenticated() -> None:
    print_step("Checking GitHub CLI authentication")
    result = run(["gh", "auth", "status"], check=False)
    if result.returncode != 0:
        raise StepError(
            "GitHub CLI is not authenticated. Run `gh auth login` first.\n"
            f"STDOUT:\n{result.stdout}\nSTDERR:\n{result.stderr}"
        )
    print_step("GitHub CLI authentication looks OK")



def get_current_branch(repo_path: pathlib.Path) -> str:
    return run(["git", "branch", "--show-current"], cwd=repo_path).stdout.strip()



def local_branch_exists(repo_path: pathlib.Path, branch_name: str) -> bool:
    result = run(["git", "show-ref", "--verify", f"refs/heads/{branch_name}"], cwd=repo_path, check=False)
    return result.returncode == 0



def remote_branch_exists(repo_path: pathlib.Path, branch_name: str) -> bool:
    result = run(["git", "ls-remote", "--heads", "origin", branch_name], cwd=repo_path, check=False)
    return bool((result.stdout or "").strip())


def branch_exists_local_or_remote(repo_path: pathlib.Path, branch_name: str) -> bool:
    return local_branch_exists(repo_path, branch_name) or remote_branch_exists(repo_path, branch_name)


def detect_default_remote_branch(repo_path: pathlib.Path) -> str | None:
    result = run(["git", "symbolic-ref", "refs/remotes/origin/HEAD"], cwd=repo_path, check=False)
    ref = (result.stdout or "").strip()
    prefix = "refs/remotes/origin/"
    if ref.startswith(prefix):
        return ref[len(prefix):]
    return None


def resolve_base_branch(repo_path: pathlib.Path, requested_base_branch: str) -> str:
    normalized = (requested_base_branch or "").strip()

    if normalized and normalized.lower() != "auto":
        if branch_exists_local_or_remote(repo_path, normalized):
            return normalized

        fallback_candidates: list[str] = []
        if normalized not in {"master", "main"}:
            fallback_candidates.append(normalized)
        fallback_candidates.extend(["master", "main"])

        for candidate in fallback_candidates:
            if branch_exists_local_or_remote(repo_path, candidate):
                print_warn(
                    f"Requested base branch `{normalized}` was not found. Falling back to `{candidate}`."
                )
                return candidate

        raise StepError(
            f"Could not find requested base branch `{normalized}` or fallback branches `master` / `main`."
        )

    remote_default = detect_default_remote_branch(repo_path)
    candidates: list[str] = []
    if remote_default:
        candidates.append(remote_default)
    candidates.extend(["master", "main"])

    seen: set[str] = set()
    for candidate in candidates:
        if candidate in seen:
            continue
        seen.add(candidate)
        if branch_exists_local_or_remote(repo_path, candidate):
            return candidate

    raise StepError("Could not detect a base branch. Neither `master` nor `main` exists locally/remotely.")


# ---------- GitHub folder loading ----------


_GITHUB_TREE_RE = re.compile(
    r"^https://github\.com/(?P<owner>[^/]+)/(?P<repo>[^/]+)/tree/(?P<ref>[^/]+)/(?P<path>.+)$"
)


@dataclass
class GitHubFolder:
    owner: str
    repo: str
    ref: str
    path: str



def is_probable_url(value: str) -> bool:
    return value.startswith("http://") or value.startswith("https://")



def parse_github_folder_url(url: str) -> GitHubFolder:
    match = _GITHUB_TREE_RE.match(url)
    if not match:
        raise StepError(
            "Unsupported GitHub folder URL format. Expected something like\n"
            "https://github.com/OWNER/REPO/tree/BRANCH/path/to/folder"
        )

    return GitHubFolder(
        owner=match.group("owner"),
        repo=match.group("repo"),
        ref=match.group("ref"),
        path=match.group("path"),
    )



def gh_api_json(endpoint: str, repo_path: pathlib.Path | None = None) -> object:
    result = run_with_retry(["gh", "api", endpoint], cwd=repo_path)
    return json.loads(result.stdout)



def fetch_folder_specs_from_github(url: str, target_dir: pathlib.Path, repo_path: pathlib.Path) -> list[pathlib.Path]:
    folder = parse_github_folder_url(url)
    print_step(
        f"Downloading commit specs from GitHub folder {folder.owner}/{folder.repo}:{folder.ref}/{folder.path}"
    )

    endpoint = f"repos/{folder.owner}/{folder.repo}/contents/{quote(folder.path)}?ref={quote(folder.ref)}"
    listing = gh_api_json(endpoint, repo_path)

    if not isinstance(listing, list):
        raise StepError("GitHub API did not return a folder listing.")

    target_dir.mkdir(parents=True, exist_ok=True)
    paths: list[pathlib.Path] = []

    for item in listing:
        if item.get("type") != "file":
            continue
        name = item.get("name", "")
        if not name.lower().endswith(".md"):
            continue

        file_path = item.get("path")
        file_endpoint = f"repos/{folder.owner}/{folder.repo}/contents/{quote(file_path)}?ref={quote(folder.ref)}"
        file_json = gh_api_json(file_endpoint, repo_path)
        content = file_json.get("content")
        encoding = file_json.get("encoding")
        if encoding != "base64" or not isinstance(content, str):
            raise StepError(f"Could not decode GitHub file: {file_path}")

        decoded = base64.b64decode(content).decode("utf-8")
        output_path = target_dir / name
        output_path.write_text(decoded, encoding="utf-8")
        paths.append(output_path)
        print_info(f"Downloaded {name}")

    if not paths:
        raise StepError("No markdown files were found in the provided GitHub folder.")

    return paths



def collect_commit_spec_paths(source_text: str, working_dir: pathlib.Path, repo_path: pathlib.Path) -> list[pathlib.Path]:
    if is_probable_url(source_text):
        return fetch_folder_specs_from_github(source_text, working_dir / "downloaded_commit_specs", repo_path)

    local_dir = pathlib.Path(source_text).expanduser().resolve()
    if not local_dir.exists() or not local_dir.is_dir():
        raise StepError(f"Commit specs path does not exist or is not a directory: {local_dir}")

    paths = [path for path in local_dir.iterdir() if path.is_file() and path.suffix.lower() == ".md"]
    if not paths:
        raise StepError(f"No markdown files found in: {local_dir}")
    return paths


# ---------- Commit spec parsing ----------


def parse_title(content: str, fallback_name: str) -> str:
    for line in content.splitlines():
        stripped = line.strip()
        if stripped.startswith("#"):
            return stripped.lstrip("#").strip()
    return fallback_name



def parse_suggested_commit_message(content: str, fallback_title: str) -> str:
    match = re.search(
        r"Suggested commit message\s*```(?:text)?\s*(.*?)\s*```",
        content,
        flags=re.IGNORECASE | re.DOTALL,
    )
    if match:
        message = match.group(1).strip()
        if message:
            first_line = message.splitlines()[0].strip()
            if first_line:
                return first_line
    return fallback_title



def load_commit_specs(paths: list[pathlib.Path]) -> list[CommitSpec]:
    specs: list[CommitSpec] = []
    for path in sort_commit_spec_paths(paths):
        body = path.read_text(encoding="utf-8")
        index = extract_first_natural_number(path.name)
        title = parse_title(body, path.stem)
        commit_message = parse_suggested_commit_message(body, title)
        specs.append(
            CommitSpec(
                index=index,
                name=path.name,
                title=title,
                body=body,
                source=str(path),
                suggested_commit_message=commit_message,
            )
        )
    return specs



def filter_specs_from_start(specs: list[CommitSpec], start_from: int) -> list[CommitSpec]:
    filtered = [spec for spec in specs if spec.index >= start_from]
    if not filtered:
        raise StepError(f"No commit specs found at or after start index {start_from}.")
    return filtered


# ---------- State ----------


def make_new_state(
    repo_path: pathlib.Path,
    spec_source: str,
    base_branch: str,
    work_branch: str,
    specs: list[CommitSpec],
    start_from: int,
    skip_final_pr: bool,
) -> StateSnapshot:
    now = datetime.utcnow().isoformat() + "Z"
    return StateSnapshot(
        version=1,
        repo_path=str(repo_path),
        spec_source=spec_source,
        base_branch=base_branch,
        work_branch=work_branch,
        created_at=now,
        updated_at=now,
        status="running",
        start_from=start_from,
        ordered_spec_names=[spec.name for spec in specs],
        ordered_spec_indices=[spec.index for spec in specs],
        completed_indices=[],
        current_index=None,
        pushed=False,
        final_pr_url=None,
        final_review_path=None,
        final_pr_created=False,
        skip_final_pr=skip_final_pr,
        last_error=None,
    )



def load_state(state_path: pathlib.Path) -> StateSnapshot:
    data = json.loads(state_path.read_text(encoding="utf-8"))
    return StateSnapshot(**data)



def save_state(state_path: pathlib.Path, state: StateSnapshot) -> None:
    state.updated_at = datetime.utcnow().isoformat() + "Z"
    state_path.parent.mkdir(parents=True, exist_ok=True)
    temp_path = state_path.with_suffix(state_path.suffix + ".tmp")
    temp_path.write_text(json.dumps(asdict(state), indent=2, ensure_ascii=False), encoding="utf-8")
    temp_path.replace(state_path)



def mark_state_error(state_path: pathlib.Path, state: StateSnapshot, message: str) -> None:
    state.status = "failed"
    state.last_error = message
    save_state(state_path, state)



def mark_state_running(state_path: pathlib.Path, state: StateSnapshot) -> None:
    state.status = "running"
    state.last_error = None
    save_state(state_path, state)



def mark_state_complete(state_path: pathlib.Path, state: StateSnapshot) -> None:
    state.status = "complete"
    state.current_index = None
    state.last_error = None
    save_state(state_path, state)



def choose_resume_mode(existing_state: StateSnapshot) -> str:
    print_header("EXISTING STATE FOUND")
    print_info(f"Work branch: {existing_state.work_branch}")
    print_info(f"Status: {existing_state.status}")
    print_info(f"Completed indices: {existing_state.completed_indices or 'none'}")
    if existing_state.last_error:
        print_info(f"Last error: {existing_state.last_error}")
    print_info(f"State created at: {existing_state.created_at}")
    print_info(f"State updated at: {existing_state.updated_at}")

    while True:
        answer = input(
            "Choose: [r]esume existing run, [n]ew run with new branch, [a]bort: "
        ).strip().lower()
        if answer in {"r", "resume"}:
            return "resume"
        if answer in {"n", "new"}:
            return "new"
        if answer in {"a", "abort"}:
            return "abort"
        print("Please answer r, n, or a.")



def validate_resume_state(state: StateSnapshot, repo_path: pathlib.Path) -> None:
    if pathlib.Path(state.repo_path).resolve() != repo_path.resolve():
        raise ResumeRequiredError(
            "The saved state belongs to a different repository path."
        )



def infer_next_index(specs: list[CommitSpec], completed_indices: list[int]) -> int | None:
    completed_set = set(completed_indices)
    for spec in specs:
        if spec.index not in completed_set:
            return spec.index
    return None


# ---------- Prompt building ----------


def ensure_rule_files(repo_path: pathlib.Path) -> list[str]:
    available: list[str] = []
    for relative in RULE_FILES:
        if (repo_path / relative).exists():
            available.append(relative)
        else:
            print_warn(f"Rule file not found, will skip in prompt: {relative}")
    return available



def build_codex_implementation_prompt(spec: CommitSpec, available_rule_files: list[str]) -> str:
    rules_block = "\n".join(f"- {path}" for path in available_rule_files) or "- (none found)"
    return textwrap.dedent(
        f"""
        You are working inside the local git repository.

        Before editing anything, read these project guidance files if they exist:
        {rules_block}

        Your job is to implement exactly one commit-sized change described below.

        Hard requirements:
        - Implement only the current commit spec.
        - Respect the project rules and contribution guidance.
        - Do not create commits yourself.
        - Do not create or merge pull requests yourself.
        - Do not rewrite earlier commit history.
        - Leave the working tree with code changes only; the outer script will handle git commit / push / PR.
        - In the final message, provide:
          1) a short summary of what changed,
          2) the files changed,
          3) what you validated,
          4) any remaining risk or uncertainty.

        Current commit spec file: {spec.name}
        Suggested commit message: {spec.suggested_commit_message}

        Begin commit spec:
        ------------------
        {spec.body}
        ------------------
        """
    ).strip() + "\n"



def run_codex_for_spec(
    repo_path: pathlib.Path,
    artifacts_dir: pathlib.Path,
    spec: CommitSpec,
    available_rule_files: list[str],
) -> pathlib.Path:
    spec_dir = artifacts_dir / f"{spec.index:02d}_{safe_branch_slug(spec.name)}"
    spec_dir.mkdir(parents=True, exist_ok=True)

    prompt_path = spec_dir / "implementation_prompt.txt"
    summary_path = spec_dir / "codex_implementation_summary.txt"
    stderr_path = spec_dir / "codex_implementation_stderr.txt"

    prompt = build_codex_implementation_prompt(spec, available_rule_files)
    prompt_path.write_text(prompt, encoding="utf-8")

    print_step(f"Sending commit {spec.index} to Codex")
    command = [
        "codex",
        "exec",
        "--cd",
        str(repo_path),
        "--full-auto",
        "--output-last-message",
        str(summary_path),
        "-",
    ]

    process = subprocess.run(
        command,
        cwd=str(repo_path),
        text=True,
        input=prompt,
        capture_output=True,
    )

    stderr_path.write_text(process.stderr or "", encoding="utf-8")

    if process.stdout:
        summary_path.write_text(process.stdout, encoding="utf-8")

    if process.returncode != 0:
        raise StepError(
            f"Codex failed for {spec.name}.\nSTDOUT:\n{process.stdout}\nSTDERR:\n{process.stderr}"
        )

    print_step(f"Codex finished commit {spec.index}")
    return spec_dir


# ---------- Git workflow ----------


def checkout_clean_base(repo_path: pathlib.Path, base_branch: str) -> None:
    print_step(f"Checking out {base_branch}")
    run(["git", "checkout", base_branch], cwd=repo_path)
    run_with_retry(["git", "pull", "--ff-only", "origin", base_branch], cwd=repo_path)



def create_branch(repo_path: pathlib.Path, branch_name: str) -> None:
    run(["git", "checkout", "-b", branch_name], cwd=repo_path)



def checkout_branch(repo_path: pathlib.Path, branch_name: str) -> None:
    run(["git", "checkout", branch_name], cwd=repo_path)



def create_work_branch_name(specs: list[CommitSpec]) -> str:
    if specs:
        first = specs[0].index
        last = specs[-1].index
        body = f"{first:02d}-to-{last:02d}"
    else:
        body = datetime.now().strftime("%Y%m%d-%H%M%S")
    return f"auto/series-{body}-{random_suffix()}"



def ensure_changes_exist(repo_path: pathlib.Path) -> None:
    result = run(["git", "status", "--porcelain"], cwd=repo_path)
    if not result.stdout.strip():
        raise StepError("Codex finished without producing any file changes.")



def git_commit_all(repo_path: pathlib.Path, commit_message: str) -> str:
    run(["git", "add", "-A"], cwd=repo_path)
    run(["git", "commit", "-m", commit_message], cwd=repo_path)
    sha = run(["git", "rev-parse", "HEAD"], cwd=repo_path).stdout.strip()
    return sha



def push_branch(repo_path: pathlib.Path, branch_name: str) -> None:
    if remote_branch_exists(repo_path, branch_name):
        run_with_retry(["git", "push", "origin", branch_name], cwd=repo_path)
    else:
        run_with_retry(["git", "push", "-u", "origin", branch_name], cwd=repo_path)



def build_final_pr_body(specs: list[CommitSpec]) -> str:
    bullet_lines = "\n".join(f"- {spec.index:02d}: {spec.title}" for spec in specs)
    return textwrap.dedent(
        f"""
        Automated PR containing the full commit-spec series.

        Included logical commits:
        {bullet_lines}

        This PR was created by the local Codex orchestration script.
        Review the branch commit-by-commit before merging.
        """
    ).strip()



def create_pull_request(
    repo_path: pathlib.Path,
    base_branch: str,
    branch_name: str,
    title: str,
    body: str,
) -> str:
    print_step("Creating final pull request")
    result = run_with_retry(
        [
            "gh",
            "pr",
            "create",
            "--base",
            base_branch,
            "--head",
            branch_name,
            "--title",
            title,
            "--body",
            body,
        ],
        cwd=repo_path,
    )
    pr_url = result.stdout.strip().splitlines()[-1].strip()
    if not pr_url.startswith("http"):
        raise StepError(f"Could not parse PR URL from gh output: {result.stdout}")
    print_step(f"PR created: {pr_url}")
    return pr_url



def get_repo_slug(repo_path: pathlib.Path) -> str:
    result = run(["git", "remote", "get-url", "origin"], cwd=repo_path)
    remote = result.stdout.strip()

    ssh_match = re.match(r"git@github\.com:(?P<owner>[^/]+)/(?P<repo>.+?)(?:\.git)?$", remote)
    if ssh_match:
        return f"{ssh_match.group('owner')}/{ssh_match.group('repo')}"

    https_match = re.match(r"https://github\.com/(?P<owner>[^/]+)/(?P<repo>.+?)(?:\.git)?$", remote)
    if https_match:
        return f"{https_match.group('owner')}/{https_match.group('repo')}"

    raise StepError(f"Unsupported origin remote URL format: {remote}")


# ---------- Orchestration ----------


def resolve_start_from(args_start_from: int | None, specs: list[CommitSpec]) -> int:
    default_index = specs[0].index
    if args_start_from is not None:
        return args_start_from

    text_value = ask(
        "Start execution from commit index (natural number from filename)",
        str(default_index),
    )
    try:
        value = int(text_value)
    except ValueError as exc:
        raise StepError(f"Invalid start index: {text_value}") from exc
    return value



def restore_or_prepare_branch(repo_path: pathlib.Path, state: StateSnapshot, is_resume: bool) -> None:
    if is_resume:
        if not local_branch_exists(repo_path, state.work_branch):
            raise StepError(
                f"Saved work branch does not exist locally: {state.work_branch}"
            )
        checkout_branch(repo_path, state.work_branch)
        print_step(f"Resumed work branch: {state.work_branch}")
        return

    checkout_clean_base(repo_path, state.base_branch)
    if local_branch_exists(repo_path, state.work_branch):
        raise StepError(f"Work branch already exists locally: {state.work_branch}")
    create_branch(repo_path, state.work_branch)
    print(state.work_branch)



def choose_state_path(repo_path: pathlib.Path, cli_state_path: str | None) -> pathlib.Path:
    if cli_state_path:
        return pathlib.Path(cli_state_path).expanduser().resolve()
    return get_default_state_path(repo_path)



def load_or_initialize_run(
    repo_path: pathlib.Path,
    spec_source_text: str,
    base_branch: str,
    state_path: pathlib.Path,
    skip_final_pr: bool,
    args_start_from: int | None,
) -> tuple[list[CommitSpec], StateSnapshot, bool]:
    artifacts_dir = get_artifacts_dir(repo_path)
    working_dir = artifacts_dir / "inputs"
    spec_paths = collect_commit_spec_paths(spec_source_text, working_dir, repo_path)
    all_specs = load_commit_specs(spec_paths)
    if not all_specs:
        raise StepError("No commit specs were loaded.")

    if state_path.exists():
        existing_state = load_state(state_path)
        validate_resume_state(existing_state, repo_path)
        mode = choose_resume_mode(existing_state)
        if mode == "abort":
            raise StepError("Aborted by user.")
        if mode == "resume":
            if existing_state.spec_source != spec_source_text:
                print_warn("Spec source differs from the saved state. The saved spec source will be used for consistency.")
            resumed_specs = filter_specs_from_start(all_specs, existing_state.start_from)
            expected_names = [spec.name for spec in resumed_specs]
            if expected_names != existing_state.ordered_spec_names:
                raise StepError(
                    "Commit spec ordering/content no longer matches the saved state. Use a new run instead."
                )
            existing_state.skip_final_pr = skip_final_pr
            existing_state.base_branch = base_branch
            mark_state_running(state_path, existing_state)
            return resumed_specs, existing_state, True

        print_step("Starting a fresh run. The previous state file will be replaced.")

    start_from = resolve_start_from(args_start_from, all_specs)
    filtered_specs = filter_specs_from_start(all_specs, start_from)
    work_branch = create_work_branch_name(filtered_specs)
    state = make_new_state(
        repo_path=repo_path,
        spec_source=spec_source_text,
        base_branch=base_branch,
        work_branch=work_branch,
        specs=filtered_specs,
        start_from=start_from,
        skip_final_pr=skip_final_pr,
    )
    save_state(state_path, state)
    return filtered_specs, state, False



def process_specs(
    repo_path: pathlib.Path,
    artifacts_dir: pathlib.Path,
    state_path: pathlib.Path,
    state: StateSnapshot,
    specs: list[CommitSpec],
    available_rule_files: list[str],
) -> None:
    completed_set = set(state.completed_indices)

    for spec in specs:
        if spec.index in completed_set:
            print_info(f"Skipping already completed commit {spec.index:02d}: {spec.name}")
            continue

        print_header(f"COMMIT {spec.index}: {spec.title}")
        state.current_index = spec.index
        mark_state_running(state_path, state)

        try:
            ensure_git_clean(repo_path)
            run_codex_for_spec(repo_path, artifacts_dir, spec, available_rule_files)
            ensure_changes_exist(repo_path)
            commit_sha = git_commit_all(repo_path, spec.suggested_commit_message)
            print_step(f"Created local commit {commit_sha}")
            state.completed_indices.append(spec.index)
            state.current_index = None
            save_state(state_path, state)
            print(f"commit {spec.index}-ый выполнен")
        except Exception as exc:
            state.last_error = f"Commit {spec.index} failed: {exc}"
            state.status = "failed"
            save_state(state_path, state)
            print_warn("Stopping on failure. The work branch was preserved so you can inspect it.")
            print_warn(f"Current work branch: {state.work_branch}")
            raise StepError(f"Commit {spec.index} failed: {exc}") from exc



def maybe_push_branch(repo_path: pathlib.Path, state_path: pathlib.Path, state: StateSnapshot) -> None:
    if state.pushed:
        print_info("Work branch was already pushed earlier. Skipping push.")
        return

    print_header("PUSHING WORK BRANCH")
    push_branch(repo_path, state.work_branch)
    state.pushed = True
    save_state(state_path, state)
    print_step(f"Work branch pushed: {state.work_branch}")



def maybe_create_final_pr(
    repo_path: pathlib.Path,
    state_path: pathlib.Path,
    state: StateSnapshot,
    specs: list[CommitSpec],
) -> str | None:
    if state.skip_final_pr:
        print("Final PR creation was skipped.")
        print(f"Review this branch manually and merge later: {state.work_branch}")
        return None

    if state.final_pr_url:
        print_info(f"Final PR already exists: {state.final_pr_url}")
        return state.final_pr_url

    pr_title = f"Automated commit series: {specs[0].index:02d}-{specs[-1].index:02d}"
    pr_body = build_final_pr_body(specs)
    pr_url = create_pull_request(
        repo_path=repo_path,
        base_branch=state.base_branch,
        branch_name=state.work_branch,
        title=pr_title,
        body=pr_body,
    )
    state.final_pr_url = pr_url
    state.final_pr_created = True
    save_state(state_path, state)
    return pr_url



def main() -> int:
    parser = argparse.ArgumentParser(
        description="Automate commit-spec -> Codex -> single branch workflow with resume support."
    )
    parser.add_argument("--repo-path", help="Local path to the git repository")
    parser.add_argument("--spec-source", help="Local folder path or GitHub folder URL with commit markdown files")
    parser.add_argument("--base-branch", default=DEFAULT_BASE_BRANCH)
    parser.add_argument("--state-file", help="Path to the persisted state JSON file")
    parser.add_argument(
        "--skip-final-pr",
        action="store_true",
        help="Do not create a final GitHub pull request; only push the branch.",
    )
    parser.add_argument(
        "--start-from",
        type=int,
        help="Start from the first commit spec whose filename number is >= this value.",
    )
    args = parser.parse_args()

    repo_path_text = args.repo_path or ask("Local path to the repository")
    spec_source_text = args.spec_source or ask(
        "Path to the folder with commit specs OR GitHub folder URL"
    )

    repo_path = normalize_repo_path(repo_path_text)
    artifacts_dir = get_artifacts_dir(repo_path)
    state_path = choose_state_path(repo_path, args.state_file)

    print_header("PRECHECKS")
    ensure_tool_exists("git")
    ensure_tool_exists("gh")
    ensure_tool_exists("codex")
    ensure_gh_authenticated()
    ensure_codex_authenticated(repo_path)

    resolved_base_branch = resolve_base_branch(repo_path, args.base_branch)
    print_step(f"Using base branch: {resolved_base_branch}")

    print_header("LOADING COMMIT SPECS")
    specs, state, is_resume = load_or_initialize_run(
        repo_path=repo_path,
        spec_source_text=spec_source_text,
        base_branch=resolved_base_branch,
        state_path=state_path,
        skip_final_pr=args.skip_final_pr,
        args_start_from=args.start_from,
    )

    for spec in specs:
        status = "done" if spec.index in set(state.completed_indices) else "todo"
        print_info(f"{spec.index:02d} -> {spec.name} [{status}]")

    available_rule_files = ensure_rule_files(repo_path)
    repo_slug = get_repo_slug(repo_path)
    print_step(f"Repository: {repo_slug}")

    print_header("PREPARING WORK BRANCH")
    try:
        ensure_git_clean(repo_path)
    except DirtyWorktreeError as exc:
        mark_state_error(state_path, state, str(exc))
        raise

    restore_or_prepare_branch(repo_path, state, is_resume)
    if not is_resume:
        print(state.work_branch)

    process_specs(
        repo_path=repo_path,
        artifacts_dir=artifacts_dir,
        state_path=state_path,
        state=state,
        specs=specs,
        available_rule_files=available_rule_files,
    )

    print("все коммиты совершены")

    maybe_push_branch(repo_path, state_path, state)

    pr_url = maybe_create_final_pr(
        repo_path=repo_path,
        state_path=state_path,
        state=state,
        specs=specs,
    )
    if pr_url:
        print(f"Open PR URL: {pr_url}")
        print("PR created but NOT merged. Review it commit-by-commit and merge manually if it looks good.")

    mark_state_complete(state_path, state)
    print_step(f"State file saved at: {state_path}")
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except KeyboardInterrupt:
        print("Interrupted by user.")
        raise SystemExit(130)
    except StepError as exc:
        print(f"ERROR: {exc}", file=sys.stderr)
        raise SystemExit(1)
