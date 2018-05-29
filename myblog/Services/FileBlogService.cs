using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using myblog.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.XPath;
using Microsoft.FSharp.Collections;

namespace myblog
{
    public class FileBlogService : IBlogService
    {
        private List<Post> _cache = new List<Post>();
        private IHttpContextAccessor _contextAccessor;
        private string _folder;

        public FileBlogService(IHostingEnvironment env, IHttpContextAccessor contextAccessor)
        {
            _folder = Path.Combine(env.WebRootPath, "posts");
            _contextAccessor = contextAccessor;

            Initialize();
        }

        public virtual Task<IEnumerable<Post>> GetPosts(int count, int skip = 0)
        {
            bool isAdmin = IsAdmin();

            var posts = _cache
                .Where(p => p.PubDate <= DateTime.UtcNow && (p.IsPublished || isAdmin))
                .Skip(skip)
                .Take(count);

            return Task.FromResult(posts);
        }

        public virtual Task<IEnumerable<Post>> GetPostsByCategory(string category)
        {
            bool isAdmin = IsAdmin();

            var posts = from p in _cache
                        where p.PubDate <= DateTime.UtcNow && (p.IsPublished || isAdmin)
                        where p.Categories.Contains(category, StringComparer.OrdinalIgnoreCase)
                        select p;

            return Task.FromResult(posts);

        }

        public virtual Task<Post> GetPostBySlug(string slug)
        {
            var post = _cache.FirstOrDefault(p => p.Slug.Equals(slug, StringComparison.OrdinalIgnoreCase));
            bool isAdmin = IsAdmin();

            if (post != null && post.PubDate <= DateTime.UtcNow && (post.IsPublished || isAdmin))
            {
                return Task.FromResult(post);
            }

            return Task.FromResult<Post>(null);
        }

        public virtual Task<Post> GetPostById(string id)
        {
            var post = _cache.FirstOrDefault(p => p.ID.Equals(id, StringComparison.OrdinalIgnoreCase));
            bool isAdmin = IsAdmin();

            if (post != null && post.PubDate <= DateTime.UtcNow && (post.IsPublished || isAdmin))
            {
                return Task.FromResult(post);
            }

            return Task.FromResult<Post>(null);
        }

        public virtual Task<IEnumerable<string>> GetCategories()
        {
            bool isAdmin = IsAdmin();

            var categories = _cache
                .Where(p => p.IsPublished || isAdmin)
                .SelectMany(post => post.Categories)
                .Select(cat => cat.ToLowerInvariant())
                .Distinct();

            return Task.FromResult(categories);
        }

        public async Task SavePost(Post post)
        {
            string filePath = GetFilePath(post);
            var postSaved = PostModule.UpdatedNow(post);

            XDocument doc = new XDocument(
                            new XElement("post",
                                new XElement("title", postSaved.Title),
                                new XElement("slug", postSaved.Slug),
                                new XElement("pubDate", postSaved.PubDate.ToString("yyyy-MM-dd HH:mm:ss")),
                                new XElement("lastModified", postSaved.LastModified.ToString("yyyy-MM-dd HH:mm:ss")),
                                new XElement("excerpt", postSaved.Excerpt),
                                new XElement("content", postSaved.Content),
                                new XElement("ispublished", postSaved.IsPublished),
                                new XElement("categories", string.Empty),
                                new XElement("comments", string.Empty)
                            ));

            XElement categories = doc.XPathSelectElement("post/categories");
            foreach (string category in post.Categories)
            {
                categories.Add(new XElement("category", category));
            }

            XElement comments = doc.XPathSelectElement("post/comments");
            foreach (Comment comment in post.Comments)
            {
                comments.Add(
                    new XElement("comment",
                        new XElement("author", comment.Author),
                        new XElement("email", comment.Email),
                        new XElement("date", comment.PubDate.ToString("yyyy-MM-dd HH:m:ss")),
                        new XElement("content", comment.Content),
                        //new XAttribute("isAdmin", comment.IsAdmin),
                        new XAttribute("id", comment.ID)
                    ));
            }

            using (var fs = new FileStream(filePath, FileMode.Create, FileAccess.ReadWrite))
            {
                await doc.SaveAsync(fs, SaveOptions.None, CancellationToken.None).ConfigureAwait(false);
            }

            // not equality !!!
            if (!_cache.Exists(p => p.ID == post.ID))
            {
                _cache.Add(post);
                SortCache();
            }
            else
            {
                var toUpdate = _cache.Find(p => p.ID == post.ID);
                _cache.Remove(toUpdate);
                _cache.Add(post);
                SortCache();
            }
            
        }

        public Task DeletePost(Post post)
        {
            string filePath = GetFilePath(post);

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            if (_cache.Contains(post))
            {
                _cache.Remove(post);
            }

            return Task.CompletedTask;
        }

        public async Task<string> SaveFile(byte[] bytes, string fileName, string suffix = null)
        {
            suffix = suffix ?? DateTime.UtcNow.Ticks.ToString();

            string ext = Path.GetExtension(fileName);
            string name = Path.GetFileNameWithoutExtension(fileName);

            string relative = $"files/{name}_{suffix}{ext}";
            string absolute = Path.Combine(_folder, relative);
            string dir = Path.GetDirectoryName(absolute);

            Directory.CreateDirectory(dir);
            using (var writer = new FileStream(absolute, FileMode.CreateNew))
            {
                await writer.WriteAsync(bytes, 0, bytes.Length).ConfigureAwait(false);
            }

            return "/posts/" + relative;
        }

        private string GetFilePath(Post post)
        {
            return Path.Combine(_folder, post.ID + ".xml");
        }

        private void Initialize()
        {
            LoadPosts();
            SortCache();
        }

        private void LoadPosts()
        {
            if (!Directory.Exists(_folder))
                Directory.CreateDirectory(_folder);

            // Can this be done in parallel to speed it up?
            foreach (string file in Directory.EnumerateFiles(_folder, "*.xml", SearchOption.TopDirectoryOnly))
            {
                XElement doc = XElement.Load(file);

                Post post = new Post(
                    iD: Path.GetFileNameWithoutExtension(file),
                    title: ReadValue(doc, "title"),
                    excerpt: ReadValue(doc, "excerpt"),
                    content: ReadValue(doc, "content"),
                    slug: ReadValue(doc, "slug").ToLowerInvariant(),
                    pubDate: DateTime.Parse(ReadValue(doc, "pubDate")),
                    lastModified: DateTime.Parse(ReadValue(doc, "lastModified", DateTime.Now.ToString())),
                    isPublished: bool.Parse(ReadValue(doc, "ispublished", "true")),
                    categories: Microsoft.FSharp.Collections.FSharpList<string>.Empty,
                    comments: Microsoft.FSharp.Collections.FSharpList<Comment>.Empty
                );

                post  = LoadCategories(post, doc);
                post = LoadComments(post, doc);
                _cache.Add(post);
            }
        }

        private static Post LoadCategories(Post post, XElement doc)
        {
            XElement categories = doc.Element("categories");
            if (categories == null)
                return post;

            List<string> list = new List<string>();

            foreach (var node in categories.Elements("category"))
            {
                list.Add(node.Value);
            }

            return PostModule.WithCategories(string.Join(",", list), post);
        }

        private static Post LoadComments(Post post, XElement doc)
        {
            var comments = doc.Element("comments");

            if (comments == null)
                return post;

            var seq = comments.Elements("comment").Select(node => new Comment(
                    iD: ReadAttribute(node, "id"),
                    author: ReadValue(node, "author"),
                    email: ReadValue(node, "email"),
                    isAdmin: bool.Parse(ReadAttribute(node, "isAdmin", "false")),
                    content: ReadValue(node, "content"),
                    pubDate: DateTime.Parse(ReadValue(node, "date", "2000-01-01"))
                ));


            return PostModule.WithComments(ListModule.OfSeq(seq), post);
        }

        private static string ReadValue(XElement doc, XName name, string defaultValue = "")
        {
            if (doc.Element(name) != null)
                return doc.Element(name).Value;

            return defaultValue;
        }

        private static string ReadAttribute(XElement element, XName name, string defaultValue = "")
        {
            if (element.Attribute(name) != null)
                return element.Attribute(name).Value;

            return defaultValue;
        }
        protected void SortCache()
        {
            _cache.Sort((p1, p2) => p2.PubDate.CompareTo(p1.PubDate));
        }

        protected bool IsAdmin()
        {
            return _contextAccessor.HttpContext?.User?.Identity.IsAuthenticated == true;
        }

    }
}
