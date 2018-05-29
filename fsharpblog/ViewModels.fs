namespace myblog.ViewModels

open System
open System.ComponentModel.DataAnnotations
open System.Text

[<CLIMutable>]
type CommentVM = {
        [<Required>]
        author:string
        [<Required>][<EmailAddress>]
        email:string
        [<Required>]
        content:string
}

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module CommentVM =
    open myblog.Models
    let toComment viewModel = 
        Comment.Create  (viewModel.author |> FSharpx.String.trim)
                        (viewModel.email |> FSharpx.String.trim)
                        (viewModel.content |> FSharpx.String.trim)


[<CLIMutable>]
type LoginViewModel= {
        [<Required>]
        UserName: string

        [<Required>]
        [<DataType(DataType.Password)>]
        Password: string

        [<Display(Name = "Remember me?")>]
        RememberMe: bool
}





[<CLIMutable>]
type PostVM = {
        [<Required>]
        ID: string

        [<Required>]
        Title: string

        Slug: string

        [<Required>]
        Excerpt: string

        [<Required>]
        Content: string
        IsPublished:bool
}

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module PostVM =
    open myblog.Models
    let toPost viewModel = 
        Post.Create  viewModel.Title
                     (viewModel.Slug |> Option.ofObj)
                     viewModel.Excerpt
                     viewModel.Content
                     viewModel.IsPublished