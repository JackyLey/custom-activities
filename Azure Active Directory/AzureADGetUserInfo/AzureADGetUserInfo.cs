using System;
using System.Data;
using System.Linq;
using Ayehu.Sdk.ActivityCreation.Extension;
using Ayehu.Sdk.ActivityCreation.Interfaces;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Graph;
using Microsoft.Graph.Auth;
using Microsoft.Identity.Client;

namespace Ayehu.Sdk.ActivityCreation
{
    public class OfficeGetUserInfo : IActivity
    {
        /// <summary>
        /// APPLICATION (CLIENT) ID
        /// </summary>
        public string appId;

        /// <summary>
        /// Directory (tenant) ID
        /// </summary>
        public string tenantId;

        /// <summary>
        /// Client secret
        /// </summary>
        /// <remarks>
        /// A secret string that the application uses to prove its identity when requesting a token. 
        /// Also can be referred to as application password.
        /// </remarks>
        public string secret;

        /// <summary>
        /// User's email to retrieve the information
        /// </summary>
        public string userEmail;

        public ICustomActivityResult Execute()
        {
            var auth = GetAuthenticated();
            var user = auth.ActiveDirectoryUsers.GetById(userEmail);
            GraphServiceClient client = new GraphServiceClient("https://graph.microsoft.com/v1.0", GetProvider());
            var officeUser = client.Users[userEmail].Request().GetAsync().Result;

            if (!string.IsNullOrEmpty(user.UserPrincipalName))
            {
                DataTable dt = new DataTable("resultSet");
                dt.Columns.Add("Id");
                dt.Columns.Add("Mail");
                dt.Columns.Add("UserPrincipalName");
                dt.Columns.Add("Surname");
                dt.Columns.Add("GivenName");
                dt.Columns.Add("UserType");
                dt.Columns.Add("MobilePhone");
                dt.Columns.Add("OfficeLocation");
                dt.Columns.Add("LicenseDetails");
                dt.Columns.Add("Settings");
                dt.Columns.Add("AccountEnabled");

                dt.Rows.Add(
                    user.Id,
                    user.Mail,
                    user.UserPrincipalName,
                    user.Inner.Surname,
                    user.Inner.GivenName,
                    user.Inner.UserType,
                    officeUser.MobilePhone,
                    officeUser.OfficeLocation,
                    officeUser.LicenseDetails,
                    officeUser.Settings,
                    user.Inner.AccountEnabled);

                return this.GenerateActivityResult(dt);
            }
            else
                throw new Exception("User not found");
        }

        private Azure.IAuthenticated GetAuthenticated()
        {
            var credentials = SdkContext.AzureCredentialsFactory.FromServicePrincipal(appId, secret, tenantId, AzureEnvironment.AzureGlobalCloud);
            var azure = Azure
                   .Configure()
                   .WithLogLevel(HttpLoggingDelegatingHandler.Level.Basic)
                   .Authenticate(credentials);

            return azure;
        }

        private ClientCredentialProvider GetProvider()
        {
            IConfidentialClientApplication confidentialClientApplication = ConfidentialClientApplicationBuilder
                .Create(appId)
                .WithTenantId(tenantId)
                .WithClientSecret(secret)
                .Build();

            return new ClientCredentialProvider(confidentialClientApplication);
        }
    }
}
