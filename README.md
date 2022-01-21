# B2B Signup

An AzureAD **multi-tenant web application** for signing up B2B users, currently in another AAD (home AAD) to the AAD this application is regsitered in (resource AAD):

1. Users from any AAD tenant may sign
2. Application verifies whether the tenant the user comes from is on an approved list
3. If so, the user is added as an external identity (B2B) to the host tenant
4. The user is also added to a pre-configured security group assigned to the tenantId they are from (see appSettiings.json)
5. User's profile data (name) is extracted from their signin token and stored in the inviting AAD

App needs the folowing ** application** API permissions:

1. GroupMember.ReadWrite.All
2. User.Invite.All
3. User.ReadWrite.All

