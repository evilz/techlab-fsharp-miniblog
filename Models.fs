module techlab_fsharp_miniblog.Models

open System
open System.ComponentModel.DataAnnotations
open System.Text


type Comment = {
     [<Required>] ID: string// Guid.NewGuid().ToString();
     [<Required>] Author:string
     [<Required;EmailAddress>] Email:string
     [<Required>] Content: string
     [<Required>] PubDate: DateTime //DateTime.UtcNow;
     IsAdmin: bool
}

module Comment = 
    
    let GetGravatar (email:string) = 
        use md5 = System.Security.Cryptography.MD5.Create()
        
        let hashBytes = email.Trim().ToLowerInvariant()
                        |> Encoding.UTF8.GetBytes
                        |> md5.ComputeHash

        hashBytes |> Array.fold  (fun sb bytes -> sb.Append(bytes.ToString("X2")) (new StringBuilder())
        sprintf "https://www.gravatar.com/avatar/%s?s=60&d=blank" (sb.ToString().ToLowerInvariant())