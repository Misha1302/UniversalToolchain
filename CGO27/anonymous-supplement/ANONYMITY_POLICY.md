# Anonymity policy

The builder copies only claim-relevant data, analyzers, protocols, and anonymous paper source. It then applies deterministic neutralization and rejects the bundle if it contains:

- author names or account handles;
- public project/repository names;
- GitHub repository URLs;
- exact 40-hex Git revisions;
- `/home`, `/mnt`, or runner workspace paths;
- `.git`, build caches, credentials, or private-key material.

Fault IDs, case IDs, oracle fingerprints, and 64-hex content digests are retained because they are experimental identities rather than author identities. The source revision is replaced consistently by `ANONYMIZED_REVISION` before analysis.
