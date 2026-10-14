# Passo a passo: do pacote até a entrega

Prazo: **20/10/2026 às 23:50**. Faça o upload no itch.io antes do último dia, para dar tempo de testar.

---

## 1. Instalar o que precisa

1. **Unity Hub** com **Unity 6 (6000.0 LTS ou mais nova)**. A **2022.3 LTS** também funciona.
2. Na instalação da Unity, marque o módulo **WebGL Build Support**.
   Se já tiver instalado sem ele: Unity Hub > Installs > engrenagem da versão > *Add modules* > WebGL Build Support.

## 2. Criar o projeto

1. Unity Hub > **New project** > template **Universal 2D** (na Unity 2022, use **2D (URP)**).
2. Nome: `RatoGuloso`. Crie e espere abrir.

## 3. Copiar os arquivos deste pacote

Com a Unity aberta, copie para dentro da pasta do projeto:

| Do pacote | Para o projeto |
|---|---|
| tudo de dentro de `Assets/` (Scripts, Editor, Art, Fonts, Resources) | `RatoGuloso/Assets/` (junta com o que já existe) |
| `Tools/`, `ItchIo/`, `README.md`, `COMO_USAR.md`, `.gitignore` | raiz do projeto, `RatoGuloso/`, ao lado de `Assets/` |

A Unity importa e compila sozinha. Espere a barrinha de progresso terminar.

> O `.gitignore` começa com ponto e pode ficar oculto. No Windows, ative "Itens ocultos" no Explorer. No Mac, use Cmd+Shift+.

## 4. Colocar seu nome

Abra `Assets/Editor/RatoGulosoSetup.cs` e troque:

```csharp
const string AuthorName = "Seu Nome Completo";
```

pelo seu nome. Ele aparece no rodapé do menu e em *Company Name*.

## 5. Montar o jogo (um clique)

Na barra de menus da Unity aparece **Rato Guloso**. Clique em:

**Rato Guloso > 1. Montar projeto (cenas, prefabs e build)**

Isso:
- configura os sprites (128 px por unidade);
- cria os prefabs em `Assets/Prefabs` (Queijo, Relogio, Ratoeira, GatoLaranja, GatoPreto);
- cria as cenas `Assets/Scenes/MainMenu`, `Game` e `GameOver`, já com HUD, menus e pausa;
- coloca as 3 cenas no Build Settings e configura o WebGL para funcionar no itch.io.

Depois de montado, tudo é editável normalmente pelo Editor. Rodar o menu de novo **recria as cenas**: qualquer mudança feita à mão nelas se perde.

## 6. Testar

1. Abra `Assets/Scenes/MainMenu` e aperte **Play**.
2. Teste com teclado e, se tiver, com o controle de Xbox.
3. Teste perder (deixe o tempo acabar e deixe os gatos te pegarem) e ganhar (coma os 12 queijos).

Se aparecer algum erro vermelho no **Console**, copie a mensagem e me mande.

## 7. Gerar a build WebGL

**Rato Guloso > 2. Gerar build WebGL** (ou File > Build Profiles > Web > Build, pasta `Builds/WebGL`).

Depois, compacte **o conteúdo** da pasta `Builds/WebGL` num `.zip`: o `index.html` precisa ficar na raiz do zip, não dentro de uma subpasta.

## 8. Página no itch.io

1. itch.io > *Upload new project*.
2. **Kind of project: HTML**.
3. Envie o `.zip` e marque **"This file will be played in the browser"**.
4. *Embed options*: **960 x 540**, marque **Fullscreen button**.
5. **Cover image:** `ItchIo/capa.png` (630 x 500).
6. **Description:** copie de `ItchIo/pagina_itch.md` e coloque **seu nome completo** e o **link do GitHub**. Sem isso, perde ponto.
7. **Screenshots:** tire 2 ou 3 prints do jogo rodando (menu, gameplay, tela de vitória). Um GIF curto também ajuda.
8. *Visibility*: **Public**. Salve.
9. Entre na jam https://itch.io/jam/projeto-intro-unity-insper-20262 e clique em **Submit your project**.
10. Abra a página numa aba anônima e jogue para conferir que roda no navegador.

## 9. GitHub (repositório público)

Na pasta do projeto `RatoGuloso/`:

```bash
git init
git add .
git commit -m "Rato Guloso - Projeto Intro Unity"
git branch -M main
git remote add origin https://github.com/SEU_USUARIO/RatoGuloso.git
git push -u origin main
```

Crie o repositório no GitHub antes (**Public**, sem README). O `.gitignore` já impede o envio de `Library/`, `Temp/` e `Builds/`.
No `README.md`, preencha o link do itch.io e seu nome.

## 10. Blackboard

Entregue os links do **itch.io** e do **GitHub** no Blackboard. Sem isso, o projeto não conta como entregue.

---

## Checklist final

- [ ] Jogo abre no navegador pelo itch.io (WebGL)
- [ ] Página do itch: capa, descrição, como jogar, **nome completo**, **link do GitHub**
- [ ] Projeto enviado para a jam
- [ ] Repositório GitHub **público** com README (créditos)
- [ ] Links entregues no Blackboard

## Problemas comuns

| Problema | Solução |
|---|---|
| Não aparece o menu "Rato Guloso" | Há erro de compilação no Console. Resolva ou me mande a mensagem. |
| Sprites rosa/roxos | Rode o menu "1. Montar projeto" de novo; ele recria o material `Assets/Art/SpriteUnlit.mat`. |
| Itch.io mostra tela preta ou erro de "decompress" | Use a build gerada pelo menu (ela liga o *decompression fallback*). Confira se o `index.html` está na raiz do zip. |
| Controle não funciona no navegador | Aperte qualquer botão do controle com a página do jogo em foco (o navegador só libera o gamepad depois disso). |
| Sem som no navegador | Clique na tela do jogo uma vez (o navegador bloqueia som até a primeira interação). |
