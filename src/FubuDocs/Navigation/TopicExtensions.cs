﻿using System;
using System.Web;
using FubuCore;
using FubuDocs.Topics;
using FubuMVC.CodeSnippets;
using FubuMVC.Core.Assets;
using FubuMVC.Core.Assets.Files;
using FubuMVC.Core.Behaviors.Chrome;
using FubuMVC.Core.Http;
using FubuMVC.Core.Registration;
using FubuMVC.Core.Runtime;
using FubuMVC.Core.UI;
using FubuMVC.Core.Urls;
using FubuMVC.Core.View;
using HtmlTags;
using System.Linq;
using System.Collections.Generic;

namespace FubuDocs.Navigation
{
    public static class TopicExtensions
    {
        public static string CurrentVersion(this IFubuPage page)
        {
            var project = page.Get<ITopicContext>().Project;
            return project == null ? "Any Version" : project.Version;
        }

        public static HtmlTag BottleSnippetFor(this IFubuPage page, string snippetName)
        {
            var project = page.Get<ITopicContext>().Project;
            var snippets = page.Get<ISnippetCache>();

            Snippet snippet = null;

            try
            {
                
                if (project == null)
                {
                    snippet = snippets.Find(snippetName);
                }
                else
                {
                    snippet = snippets.As<SnippetCache>().FindByBottle(snippetName, project.BottleName) ??
                              snippets.Find(snippetName);
                }
            }
            catch (Exception)
            {
                throw new ArgumentOutOfRangeException("snippetName", "Requested snippet '{0}' does not exist".ToFormat(snippetName));
            }

            if (snippet == null)
            {
                throw new ArgumentOutOfRangeException("snippetName", "Requested snippet '{0}' does not exist".ToFormat(snippetName));
            }

            return page.CodeSnippet(snippet);
        }

        public static HtmlTag AllProjectsTable(this IFubuPage page)
        {
            return page.Get<AllProjectsModel>().Topics;
        }


        public static IHtmlString ProjectSummary(this IFubuPage page)
        {
            var project = page.Get<ITopicContext>().Project;
            return page.Partial(project);
        }

        public static HtmlTag TableOfContents(this IFubuPage page)
        {
            return page.Get<TopicTreeBuilder>().BuildTableOfContents();
        }

        public static TagList LeftTopicNavigation(this IFubuPage page)
        {
            return page.Get<TopicTreeBuilder>().BuildLeftTopicLinks().ToTagList();
        }

        public static TagList TopTopicNavigation(this IFubuPage page)
        {
            return page.Get<TopicTreeBuilder>().BuildTopTopicLinks().ToTagList();
        }

        public static HtmlTag LinkToTopic(this IFubuPage page, string name, string title)
        {
            var context = page.Get<ITopicContext>();
            Topic topic = context.Project.FindByKey(name);
            if (topic == null)
            {
                var available = context.Project.AllTopics().Select(x => "'" + x.Key + "'").Join(", \n");

                throw new ArgumentOutOfRangeException("name", "Topic '{0}' cannot be found.  Try:\n{1}".ToFormat(name, available));
            }

            return new TopicLinkTag(page.Get<ICurrentHttpRequest>(), page.Get<FubuDocsDirectories>(), topic, title);
        }

        public static HtmlTag LinkToExternalTopic(this IFubuPage page, string name, string title)
        {
            Topic topic = TopicGraph.AllTopics.Find(name);
            if (topic == null)
            {
                return new HtmlTag("span").Text("*LINK TO " + name + "*");
            }

            return new TopicLinkTag(page.Get<ICurrentHttpRequest>(), page.Get<FubuDocsDirectories>(), topic, title);
        }


        public static HtmlTag ProjectLink(this IFubuPage page, string name)
        {
            var project = TopicGraph.AllTopics.TryFindProject(name);
            if (project == null)
            {
                return new HtmlTag("span").Text("LINK TO PROJECT '{0}'".ToFormat(name));
            }

            return new LinkTag(project.Name, page.Get<ICurrentHttpRequest>().ToRelativeUrl(page.Get<FubuDocsDirectories>(), project.Home.AbsoluteUrl)).Attr("title", project.Description);
        }

        public static string ProjectIndexUrl(this IFubuPage page, string name)
        {
            var project = TopicGraph.AllTopics.TryFindProject(name);
            if (project == null)
            {
                return "#";
            }

            return page.Get<ICurrentHttpRequest>().ToRelativeUrl(page.Get<FubuDocsDirectories>(), project.Index.AbsoluteUrl);
        }

        public static HtmlTag RootLink(this IFubuPage page, string text)
        {
            var root = page.Get<IUrlRegistry>().UrlFor<AllTopicsEndpoint>(x => x.get_topics());
            return new HtmlTag("a")
                .AddClass("root-link")
                .Attr("href", root)
                .Attr("title", text)
                .Append("span", span => span.Text(text));
        }

        public static HtmlTag ProjectLogo(this IFubuPage page)
        {
            var project = page.Get<ITopicContext>().Project;
            if (project == null)
            {
                return new HtmlTag("div").Render(false);
            }

            // TODO -- Maybe include the project logo if it's specified?
            var homeUrl =
                page.Get<ICurrentHttpRequest>().ToRelativeUrl(page.Get<FubuDocsDirectories>(), project.Home.AbsoluteUrl);
            return new HtmlTag("a")
                .Attr("href", homeUrl)
                .Attr("title", project.TagLine)
                .AddClass("project-logo")
                .Append("span", span => span.Text(project.Name));
        }

        public static HtmlTag MailingList(this IFubuPage page, string text)
        {
            var project = page.Get<ITopicContext>().Project;

            if (project == null || project.UserGroupUrl.IsEmpty()) return LiteralTag.Empty();

            return new HtmlTag("em")
                    .Append("a", a => a.Attr("href", project.UserGroupUrl).Text(text));
        }

        public static TagList SocialIcons(this IFubuPage page)
        {
            var project = page.Get<ITopicContext>().Project;

            return new TagList(determineSocialIcons(project, page.Get<IAssetUrls>()));
        }

        public static string ProjectName(this IFubuPage page)
        {
            var project = page.Get<ITopicContext>().Project;
            return project == null ? string.Empty : project.Name;
        }

        public static string TopicTitle(this IFubuPage page)
        {
            var context = page.Get<ITopicContext>();
            if (context.Current != null) return context.Current.Title;

            if (context.Project != null) return context.Project.Name;

            return string.Empty;
        }

        private static IEnumerable<HtmlTag> determineSocialIcons(ProjectRoot project, IAssetUrls urls)
        {
            if (project == null) yield break;

            if (project.TwitterHandle.IsNotEmpty())
            {
                yield return new HtmlTag("a")
                    .AddClass("ico-twitter")
                    .Attr("href", "http://twitter.com/" + project.TwitterHandle)
                    .Append("img", img =>
                    {
                        img.Attr("alt", "Twitter")
                        .Attr("src", urls.UrlForAsset(AssetFolder.images, "twitter-icon.png"));
                    });
            }

            if (project.GitHubPage.IsNotEmpty())
            {
                yield return new HtmlTag("a")
                    .AddClass("ico-github")
                    .Attr("href", project.GitHubPage)
                    .Append("img", img =>
                    {
                        img.Attr("alt", "Github")
                        .Attr("src", urls.UrlForAsset(AssetFolder.images, "github-icon.png"));
                    });
            }
        }

        public static string InnerContent(this IFubuPage page)
        {
            var chrome = page.Get<IFubuRequest>().Find<ChromeContent>().FirstOrDefault();
            return chrome == null ? string.Empty : chrome.InnerContent;
        }
    }
}