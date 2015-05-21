+ Fixed exception that would occur when trying to activate a new activity on an existing workflow (Fixes #978).
+ Updated the Checkin Schedule to have it's 'StartTime' property available to Lava (Fixes #981).
+ Updated the default ordering of transactions in Contributions tab of person profile be datetime of the transaction (Fixes #980).
+ Fixed issue with My Workflows block not showing the workflow type.
+ Fixed "Group Type" Report Field not showing any results (Fixes #977).
+ Added validation to prevent duplicate routes being created for different pages.
+ Updated person object so that PrimaryAlias property is avaialbe to be used by Lava (Fixes #973).
+ Fixed issue with Add Family block adding children as adults (Fixes #964).
+ Fixed issue with prayer approval not updating request correctly, resulting in prayer not being visible to prayer session (Fixes #969).
+ Updated communicaiton processing so that communication job will not send duplicate emails to same recipients.
+ Fixed issue with occasional deadlocks.
+ Added job for processing scheduled metrics.
+ Fixed attribute block to escape HTML in the values column (fixes #965).
+ Added access keys to edit the individual (Alt+I) and family (Alt+O) on the person details page.
+ Changed the access key for edit from Alt+E to Alt+M (modify) since Chrome already used Alt+E. (Fixes #946).
