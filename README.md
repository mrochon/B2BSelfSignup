# B2B Signup

An AzureAD **multi-tenant web application** for signing up B2B users, currently in another AAD (home AAD) to the AAD this application is regsitered in (resource AAD):

1. Users from any AAD tenant may sign
2. If the user's home tenant is on the approved list of tenants (see appSettings.json), the user is added as an external identity (invited B2B guest) to the resource tenant
4. The user is also added to a pre-configured security group assigned to the tenantId they are from (see appSettings.json)


App needs the folowing ** application** API permissions:

1. GroupMember.ReadWrite.All
2. User.Invite.All

