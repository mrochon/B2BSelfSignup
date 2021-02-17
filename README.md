# B2B Signup

An AzureAD multi-tenant web application for signing up B2B users into a host tenant:

1. Users from any AAD tenant may sign
2. Application verifies whether the tenant the user comes from is on an approved list
3. If so, the user is added as an external identity (B2B) to the host tenant
4. The user is also added to a pre-configured security group

