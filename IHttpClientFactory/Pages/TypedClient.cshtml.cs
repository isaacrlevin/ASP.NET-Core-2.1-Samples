using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using IHttpClientFactorySample.GitHub;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace IHttpClientFactorySample.Pages
{
    public class TypedClientModel : PageModel
    {
        private readonly GitHubService _gitHubService;

        public IEnumerable<GitHubIssue> LatestIssues { get; private set; }

        public bool HasIssue => LatestIssues.Any();

        public bool GetIssuesError { get; private set; }

        public TypedClientModel(GitHubService gitHubService)
        {
            _gitHubService = gitHubService;
        }

        public async Task OnGet()
        {
            try
            {
                LatestIssues = await _gitHubService.GetAspNetDocsIssues();
            }
            catch(HttpRequestException)
            {
                GetIssuesError = true;
                LatestIssues = Array.Empty<GitHubIssue>();
            }            
        }
    }
}