---
customTheme : "soat"
controls: false
---

<!-- .slide: data-background="assets/soat-1.png" -->
# Introduire du code F# au sein d’une application C#.   

---

<!-- .slide: data-background="assets/soat-2.png" -->
## Vincent Bourdon

- <i class="fab fa-github"></i> Github : https://github.com/evilz
- <i class="fab fa-twitter"></i> Twitter : @Evilznet


<!-- .slide: data-background="assets/soat-2.png" -->
## Objectifs

A partir d'un site en C# déplacer le code des modèles dans un librairie en F#

---

<!-- .slide: data-background="assets/soat-2.png" -->
## Planning

- Utilisation de VS 2017
- Manipulation de la CLI dotnet
- Organisation du code F# en namespace et module
- Utilisation de record immutable
- Création de fonctions pures
- Création de Pipeline
- Utilisation de Map/Reduce ou Folding

---

<!-- .slide: data-background="assets/soat-2.png" -->
## Step 1

Creation de l'application et la solution via la CLI

Note:
dotnet new --install MadsKristensen.AspNetCore.Miniblog
dotnet new miniblog
dotnet run
dotnet new sln
dotnet sln add myblog\myblog.csproj
md fsharpblog
dotnet new console -lang F#
dotnet sln add .\techlab-fsharp-miniblog.fsproj

---

<!-- .slide: data-background="assets/soat-2.png" -->
## Récap

La CLI dotnet permet de :
- ajouter des templates de projets
- créer des projets
- créer et manipuler des solutions 


---

<!-- .slide: data-background="assets/soat-2.png" -->
## Step 2

Déplacement du modèle `Comment`


---

<!-- .slide: data-background="assets/soat-2.png" -->
## Step 2 bis

Création du VIEW modèle `CommentVM`

---

<!-- .slide: data-background="assets/soat-2.png" -->
## Récap

- Ajouter un namespace, module = class static
- `CompilationRepresentation` pour éviter les conflits de noms
- Les records sont immutable, on peut ajouter `CLIMutable` pour obtenir un constructeur par defaut et des setter et getter.
- utilisation de la lib `FSharpx` pour faciliter le typage et le chaînage 


---

<!-- .slide: data-background="assets/soat-2.png" -->
## Step 3

Déplacement du modèle `LoginViewModel`


---

<!-- .slide: data-background="assets/soat-2.png" -->
## Step 4

Déplacement du modèle `Post`


---

<!-- .slide: data-background="assets/soat-2.png" -->
## Step 5

Fix de l'application.

---

<!-- .slide: data-background="assets/soat-1.png" -->
# QUESTION ?
