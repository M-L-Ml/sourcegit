using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;

namespace SourceGit.Models
{
    /// <summary>
    /// Represents a commit link for a remote repository.
    /// </summary>
    public readonly record struct CommitLink(string Name, string URLPrefix)
    {
    }

    public static class CommitLinkHelpers
    {
        public static string TrimDotGitSuffix(string input)
        {
            if (input.EndsWith(".git", StringComparison.OrdinalIgnoreCase))
                return input[..^4];
            return input;
        }

        public static bool TryParseUri(string url, out Uri? uri)
        {
            uri = null;
            if (string.IsNullOrWhiteSpace(url)) return false;
            if (Uri.TryCreate(url, UriKind.Absolute, out var parsed))
            {
                uri = parsed;
                return true;
            }
            return false;
        }
    }

    public readonly record struct ProviderInfo2(
        string Name,
        string ExampleHostURL,
        string HostURLMainPart,
        string UrlSubPartForCommit,
        bool NeedTrim = false
    )
    {
        public bool IsMatch(string url)
        {
            if (!CommitLinkHelpers.TryParseUri(url, out var uri) || uri is null) return false;
            return uri.Host.Equals(HostURLMainPart, StringComparison.OrdinalIgnoreCase);
        }

        public string ExtractRepo(string url)
        {
            if (!CommitLinkHelpers.TryParseUri(url, out var uri) || uri is null) return string.Empty;
            var res = uri.AbsolutePath.TrimStart('/');
            if (NeedTrim)
            {
                int idx = res.IndexOf('/') + 1;
                if (idx > 0 && idx < res.Length)
                    res = res[idx..];
            }
            res = CommitLinkHelpers.TrimDotGitSuffix(res);
            return res;
        }

        public string BuildCommitUrlPrefix(string url)
        {
            url = CommitLinkHelpers.TrimDotGitSuffix(url);
            return url + UrlSubPartForCommit;
        }
    }

    public static class CommitLinkDetails
    {
        static readonly ProviderInfo2[] Providers = new[]
        {
            new ProviderInfo2("Github", "https://github.com/", "github.com", "/commit/", false),
            new ProviderInfo2("GitLab", "https://gitlab.com/", "gitlab.com", "/-/commit/", false),
            new ProviderInfo2("Gitee", "https://gitee.com/", "gitee.com", "/commit/", false),
            new ProviderInfo2("BitBucket", "https://bitbucket.org/", "bitbucket.org", "/commits/", false),
            new ProviderInfo2("Codeberg", "https://codeberg.org/", "codeberg.org", "/commit/", false),
            new ProviderInfo2("Gitea", "https://gitea.org/", "gitea.org", "/commit/", false),
            new ProviderInfo2("sourcehut", "https://git.sr.ht/", "git.sr.ht", "/commit/", false)
        };

        private static CommitLink? TryCreateCommitLink(Remote remote)
        {
            if (!remote.TryGetVisitURL(out var url))
                return null;
            var provider = Providers.FirstOrDefault(p => p.IsMatch(url));
            if (provider.Name == null)
                return null;
            string repoName = provider.ExtractRepo(url);
            return new CommitLink($"{provider.Name} ({repoName})", provider.BuildCommitUrlPrefix(url));
        }

        public static List<CommitLink> Get(List<Remote> remotes)
        {
            return remotes.Select(static remote =>
            {
                var rr = TryCreateCommitLink(remote);
#if DEBUG
                if (remote.TryGetVisitURL(out var url))
                {
                    var commitLink = GetCommitLinkOriginalImplementionForTestPurposes(url);
                    Debug.Assert(commitLink == rr, " checking comparing with initial implementation failed, TODO: delete in future");
                }
#endif
                return rr;
            }).Where(cl => cl.HasValue).Select(cl => cl.Value).ToList();
        }

#if DEBUG
        private static CommitLink? GetCommitLinkOriginalImplementionForTestPurposes(string url)
        {
            var outs = new List<CommitLink>();
            var trimmedUrl = CommitLinkHelpers.TrimDotGitSuffix(url);
            if (url.StartsWith("https://github.com/", StringComparison.Ordinal))
                outs.Add(new($"Github ({trimmedUrl.Substring(19)})", $"{url}/commit/"));
            else if (url.StartsWith("https://gitlab.", StringComparison.Ordinal))
                outs.Add(new($"GitLab ({trimmedUrl.Substring(trimmedUrl.Substring(15).IndexOf('/') + 16)})", $"{url}/-/commit/"));
            else if (url.StartsWith("https://gitee.com/", StringComparison.Ordinal))
                outs.Add(new($"Gitee ({trimmedUrl.Substring(18)})", $"{url}/commit/"));
            else if (url.StartsWith("https://bitbucket.org/", StringComparison.Ordinal))
                outs.Add(new($"BitBucket ({trimmedUrl.Substring(22)})", $"{url}/commits/"));
            else if (url.StartsWith("https://codeberg.org/", StringComparison.Ordinal))
                outs.Add(new($"Codeberg ({trimmedUrl.Substring(21)})", $"{url}/commit/"));
            else if (url.StartsWith("https://gitea.org/", StringComparison.Ordinal))
                outs.Add(new($"Gitea ({trimmedUrl.Substring(18)})", $"{url}/commit/"));
            else if (url.StartsWith("https://git.sr.ht/", StringComparison.Ordinal))
                outs.Add(new($"sourcehut ({trimmedUrl.Substring(18)})", $"{url}/commit/"));
            return outs.FirstOrDefault();
        }
        static CommitLinkDetails()
        {
            var githubRemote = new Remote() { URL = "https://github.com/user/repo.git" };
            var links = Get(new List<Remote> { githubRemote });
            Debug.Assert(links.Count == 1, "Should find one CommitLink for Github");
            Debug.Assert(links[0].Name.StartsWith("Github"), "Provider should be Github");
            Debug.Assert(links[0].URLPrefix == "https://github.com/user/repo/commit/", "URLPrefix should be correct for Github");
            var bitbucketRemote = new Remote() { URL = "https://bitbucket.org/team/project" };
            links = Get(new List<Remote> { bitbucketRemote });
            Debug.Assert(links.Count == 1, "Should find one CommitLink for BitBucket");
            Debug.Assert(links[0].Name.StartsWith("BitBucket"), "Provider should be BitBucket");
            Debug.Assert(links[0].URLPrefix == "https://bitbucket.org/team/project/commits/", "URLPrefix should be correct for BitBucket");
            var gitlabRemote = new Remote() { URL = "https://gitlab.com/group/project.git" };
            links = Get(new List<Remote> { gitlabRemote });
            Debug.Assert(links.Count == 1, "Should find one CommitLink for GitLab");
            Debug.Assert(links[0].Name.StartsWith("GitLab"), "Provider should be GitLab");
            Debug.Assert(links[0].URLPrefix == "https://gitlab.com/group/project/-/commit/", "URLPrefix should be correct for GitLab");
        }
#endif
    }
}
