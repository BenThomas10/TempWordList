using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(WordLists.Startup))]
namespace WordLists
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
