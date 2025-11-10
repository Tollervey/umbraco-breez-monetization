
== Working directory and secret storage guidance ==

The Breez SDK requires a working directory to store wallet state and SDK artifacts. You can customize the path using the configuration key `LightningPayments:WorkingDirectory` or environment variable `LightningPayments__WorkingDirectory`.

Best practices:
- Use a dedicated secure path outside the webroot for the SDK working directory.
- Ensure the application user has read/write access and apply restrictive ACLs (e.g., chmod700 on Unix).
- Mount a persistent volume in containerized environments for durability.
- Treat wallet material as highly sensitive and back it up only to secure storage.

Example environment variable:
```
LightningPayments__WorkingDirectory=/var/lib/myapp/lightning
```
