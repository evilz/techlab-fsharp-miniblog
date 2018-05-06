namespace myblog.Models

open System
open System.ComponentModel.DataAnnotations
open System.Text
open System.Globalization
open System.Text.RegularExpressions

type Comment = {
        ID:string
        Author:string
        Email:string
        Content:string
        PubDate:DateTime
        IsAdmin:bool
}

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Comment =
    let GetGravatar (email:string) =
        
        use md5 = System.Security.Cryptography.MD5.Create()
        
        let hashBytes = email.Trim().ToLowerInvariant()
                        |> Encoding.UTF8.GetBytes
                        |> md5.ComputeHash

        // Convert the byte array to hexadecimal string
        let sb = new StringBuilder()
        for hash in hashBytes do
            sb.Append(hash.ToString("X2")) |> ignore
            

        sprintf "https://www.gravatar.com/avatar/%s?s=60&d=blank" (sb.ToString().ToLowerInvariant())
       
    let Create author email content = 
        { ID = Guid.NewGuid().ToString()
          Author = author |> FSharpx.String.trim
          Email = email |> FSharpx.String.trim
          Content = content |> FSharpx.String.trim
          PubDate = DateTime.UtcNow
          IsAdmin = false }
        
    let IsAdmin comment = { comment with IsAdmin = true }



type Post = {
        ID: string //DateTime.UtcNow.Ticks.ToString();
        Title: string
        Slug: string
        Excerpt: string
        Content: string
        PubDate: DateTime // DateTime.UtcNow;
        LastModified: DateTime// DateTime.UtcNow;
        IsPublished:bool //true;
        Categories: string seq //new List<string>();
        Comments: Comment seq // new List<Comment>();
}

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Post =
    

    let GetLink post = sprintf "/blog/%s/" post.Slug

    let AreCommentsOpen post commentsCloseAfterDays = post.PubDate.AddDays(commentsCloseAfterDays) >= DateTime.UtcNow

    let RemoveReservedUrlCharacters text =
            let reservedCharacters = ["!"; "#"; "$"; "&"; "'"; "("; ")"; "*"; ";"; "/"; ":"; ";"; "="; "?"; "@"; "["; "]"; "\""; "%"; "."; "<"; ">"; "\\"; "^"; "_"; "'"; "{"; "}"; "|"; "~"; "`"; "+" ]

            (text, reservedCharacters) ||> List.fold  (fun state chr -> state |> FSharpx.String.replace' chr "")

    let RemoveDiacritics text =
            let normalizedString = text |> FSharpx.String.normalize' NormalizationForm.FormD

            let stringBuilder = 
                (new StringBuilder(), normalizedString |> FSharpx.String.toCharArray) 
                ||> Array.fold (fun sb c -> match CharUnicodeInfo.GetUnicodeCategory(c) with
                                            | UnicodeCategory.NonSpacingMark -> sb.Append(c) 
                                            | _ -> sb                
                )

            stringBuilder.ToString().Normalize(NormalizationForm.FormC)

    let CreateSlug title =
            title
            |> FSharpx.String.toLowerInvariant
            |> FSharpx.String.replace' " " "-"
            |> RemoveDiacritics
            |> RemoveReservedUrlCharacters
            |> FSharpx.String.toLowerInvariant

        

    let RenderContent post =
        let result = post.Content

        // Set up lazy loading of images/iframes
        let result = result |> FSharpx.String.replace' " src=\"" " src=\"data:image/gif;base64,R0lGODlhAQABAIAAAP///wAAACH5BAEAAAAALAAAAAABAAEAAAICRAEAOw==\" data-src=\""

        // Youtube content embedded using this syntax: [youtube:xyzAbc123]
        let videoFormat = sprintf "<div class=\"video\"><iframe width=\"560\" height=\"315\" title=\"YouTube embed\" src=\"about:blank\" data-src=\"https://www.youtube-nocookie.com/embed/%s?modestbranding=1&amp;hd=1&amp;rel=0&amp;theme=light\" allowfullscreen></iframe></div>";
        let result = Regex.Replace(result, @"\[youtube:(.*?)\]", (fun m -> videoFormat (m.Groups.[1].Value)))
        result

    let Create title slug excerpt content isPublished = 
        let post  = { ID= DateTime.UtcNow.Ticks.ToString()
                      Title= title |> FSharpx.String.trim
                      Excerpt = excerpt |> FSharpx.String.trim
                      Content = content |> FSharpx.String.trim
                      PubDate= DateTime.UtcNow
                      LastModified= DateTime.UtcNow
                      IsPublished= isPublished
                      Comments = Seq.empty
                      Categories = Seq.empty
                      Slug = match slug with
                            | Some s -> s
                            | None -> CreateSlug title }
        post

    let Empty = Create String.Empty None String.Empty  String.Empty false


    let UpdateWith oldPost newPost = 
        { oldPost with 
                Title= newPost.Title
                Slug = newPost.Slug
                Excerpt = newPost.Excerpt
                Content = newPost.Content
                LastModified= DateTime.UtcNow
                IsPublished= newPost.IsPublished
                Categories = newPost.Categories
        }

    let WithCategories categories post = 
        { post with Categories = categories 
                                    |> FSharpx.String.splitString [|","|] StringSplitOptions.RemoveEmptyEntries
                                    |> Array.map (fun c -> c |> FSharpx.String.trim |> FSharpx.String.toLowerInvariant)
                                    |> List.ofArray
        }

    let WithComments comments post = { post with Comments = comments}

    let UpdatedNow post = { post with LastModified = DateTime.UtcNow}

    let WithPubDate pubDate post  = { post with PubDate = pubDate}

    let WithContent content post = { post with Content = content}

    let AddComment comment post = { post with Comments = seq { yield comment; yield! post.Comments } }

    let RemoveComment comment post =
        let comments = post.Comments |> Seq.filter (fun x -> x <> comment)
        { post with Comments = comments}