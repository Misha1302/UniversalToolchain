# Anonymization contract

The supplement is acceptable only when all of the following hold:

1. no version-control metadata or hosted-CI configuration is included;
2. author names, account names, public project names, personal URLs, public commit identifiers, and local absolute paths are absent;
3. paper metadata identifies only anonymous authors;
4. source and evidence are content-addressed with SHA-256 manifests;
5. the anonymized source snapshot passes project-reference validation and provider-backed clean execution;
6. the archive can be unpacked and verified without access to the originating repository;
7. blocked external-validity and performance claims remain explicitly blocked.

The sanitizer changes identifiers consistently in text and paths. It does not claim to create independent evidence or to strengthen the semantics of the evaluated verifiers.
