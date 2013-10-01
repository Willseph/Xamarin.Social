using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Auth;
using Xamarin.Utilities;

namespace Xamarin.Social.Services
{
	public class DisqusService : OAuth2Service
	{
		public DisqusService()
			: base("Disqus", "Disqus")
		{
			AuthorizeUrl = new Uri("https://disqus.com/api/oauth/2.0/authorize/");
			AccessTokenUrl = new Uri("https://disqus.com/api/oauth/2.0/access_token/");
			Scope = "read,write";
		}

		protected override Task<string> GetUsernameAsync (IDictionary<string, string> accountProperties)
		{
			var request = base.CreateRequest ("GET",
				new Uri("https://disqus.com/api/3.0/users/details.json"),
				null,
				new Account (string.Empty, accountProperties));

			return request.GetResponseAsync ().ContinueWith (reqTask => {
				var responseText = reqTask.Result.GetResponseText ();
				return WebEx.GetValueFromJson (responseText, "id");
			});
		}

		protected override Authenticator GetAuthenticator ()
		{
			return new DisqusAuthenticator(ClientId, ClientSecret, Scope, AuthorizeUrl, RedirectUrl, AccessTokenUrl, GetUsernameAsync);
		}

		public override Task<Account> ReauthorizeAsync (Account account)
		{
			var authenticator = (DisqusAuthenticator)GetAuthenticator();

			return authenticator.RefreshAccessTokenAsync (account.Properties ["refresh_token"]).ContinueWith (t => {
				var props = new Dictionary<string, string> (account.Properties);
				props ["access_token"] = t.Result ["access_token"];
				return new Account (account.Username, props, account.Cookies);
			});
		}

		public override bool SupportsReauthorization {
			get {
				return true;
			}
		}

		public override Task VerifyAsync (Account account, CancellationToken token)
		{
			return CreateRequest ("GET",
				new Uri("https://disqus.com/api/3.0/users/details.json"),
				account
			).GetResponseAsync (token).ContinueWith (t => {
				if (!t.Result.GetResponseText ().Contains ("\"id\""))
					throw new SocialException ("Unrecognized Disqus response.");
			}, token);
		}

		public override bool SupportsVerification {
			get {
				return true;
			}
		}

		class DisqusAuthenticator : OAuth2Authenticator {
			private readonly string clientId;
			private readonly string clientSecret;

			public Task<IDictionary<string, string>> RefreshAccessTokenAsync (string refreshToken)
			{
				return RequestAccessTokenAsync (new Dictionary<string, string> {
					{ "grant_type", "refresh_token" },
					{ "client_id", clientId },
					{ "client_secret", clientSecret },
					{ "refresh_token", refreshToken }
				});
			}

			public DisqusAuthenticator(string clientId, string clientSecret, string scope, Uri authorizeUrl, Uri redirectUrl, Uri accessTokenUrl, GetUsernameAsyncFunc getUsernameAsync)
				: base (clientId, clientSecret, scope, authorizeUrl, redirectUrl, accessTokenUrl, getUsernameAsync)
			{
				this.clientId = clientId;
				this.clientSecret = clientSecret;
			}
		}
	}
}