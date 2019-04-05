using System;

namespace AutomaEx.Facebook
{
    /// <summary>
    /// FacebookContext
    /// </summary>
    //https://developers.facebook.com/docs/graph-api/using-graph-api#fieldexpansion
    public partial class FacebookContext : AutomaContext
    {
        /// <summary>
        /// The base
        /// </summary>
        protected const string BASE = "https://graph.facebook.com";
        /// <summary>
        /// The bas ev
        /// </summary>
        protected const string BASEv = "https://graph.facebook.com/v3.2"; // v2.10";

        /// <summary>
        /// Initializes a new instance of the <see cref="FacebookContext" /> class.
        /// </summary>
        /// <exception cref="System.ArgumentNullException">f</exception>
        public FacebookContext()
            : base(x => new Automa(x, (ctx, driver) => new FacebookAutomation(ctx, driver)))
        {
            RequestedScope = "manage_pages,ads_management"; // //read_insights,leads_retrieval
            Logger = (x) => Console.WriteLine(x);
        }

        /// <summary>
        /// Gets or sets the requested scope.
        /// </summary>
        /// <value>
        /// The requested scope.
        /// </value>
        public string RequestedScope { get; set; }

        /// <summary>
        /// Gets or sets the application identifier.
        /// </summary>
        /// <value>
        /// The application identifier.
        /// </value>
        public string AppId { get; set; }

        /// <summary>
        /// Gets or sets the application secret.
        /// </summary>
        /// <value>
        /// The application secret.
        /// </value>
        public string AppSecret { get; set; }

        /// <summary>
        /// Gets or sets the client token.
        /// </summary>
        /// <value>The client token.</value>
        public string ClientToken { get; set; }

        private void EnsureAppIdAndSecret()
        {
            if (string.IsNullOrEmpty(AppId) || string.IsNullOrEmpty(AppSecret))
                throw new InvalidOperationException("AppId and AppSecret are required for this operation.");
        }

        private void EnsureAppIdAndToken()
        {
            if (string.IsNullOrEmpty(AppId) || string.IsNullOrEmpty(ClientToken))
                throw new InvalidOperationException("AppId and ClientToken are required for this operation.");
        }

        private void EnsureRequestedScope()
        {
            if (string.IsNullOrEmpty(RequestedScope))
                throw new InvalidOperationException("RequestedScope is required for this operation.");
        }
    }
}
