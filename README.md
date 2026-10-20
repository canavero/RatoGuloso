# Rato Guloso: A Corrida do Queijo

Jogo 2D feito na Unity para o **Projeto Introdução ao Unity** (Jogos Digitais, Insper 2026), a partir do tutorial *"Introdução Rápida"* da disciplina.

Você é um rato guloso numa cozinha. Coma **todos os queijos** antes que o tempo acabe e fuja dos gatos e das ratoeiras.

- **Jogar no navegador (itch.io):** https://canavero.itch.io/rato-guloso-a-corrida-do-queijo
- **Autor:** Enzo Canavero

## Como jogar

| Ação | Teclado | Controle (Xbox) |
|---|---|---|
| Mover | WASD / setas | analógico esquerdo ou direcional |
| Dash (gasta energia) | Espaço ou Shift | A / RB / RT |
| Pausar | P ou Esc | Start |
| Liga/desliga o som | M | Back / View |
| Navegar nos menus | setas + Enter, ou mouse | analógico + A |

- Cada **queijo** vale 10 pontos e dá **+3 segundos**. O **relógio** dá **+10 segundos**.
- **Gatos laranja** fazem patrulha. O **gato preto** persegue você quando chega perto e contorna os balcões.
- **Ratoeiras** fecham quando você pisa nelas e ficam desarmadas por alguns segundos.
- Você tem **3 vidas**. Os gatos ficam mais rápidos com o passar do tempo.
- Se você vencer, o tempo que sobrou vira pontos de bônus. O recorde e o melhor tempo ficam salvos.

## O que foi adicionado ao tutorial (rubrica)

| Item | Implementação |
|---|---|
| **Tutorial base** | Movimento com `Rigidbody2D`, coleta com `OnTriggerEnter2D`, pontuação e `GameController` estático guardando os dados entre cenas |
| **2.1 Mecânica de tempo** | Cronômetro na HUD; tempo final na tela de fim; contagem regressiva em que queijo e relógio dão tempo extra, o tempo restante vira bônus e os gatos aceleram com o tempo; bipe nos últimos 10 s |
| **2.2 Inimigos** | Ratoeiras paradas; gatos laranja com patrulha (`EnemyPatrol`); gato preto com patrulha e perseguição usando busca de caminho em grade (`EnemyChaser` + `NavGrid`) |
| **2.3 Visual** | Sprites próprios (rato, queijo, gatos, relógio, ratoeira, piso, balcões, tijolos); cores trocadas via `SpriteRenderer.color` (gato laranja, piso, balcões) |
| **2.4 Áudio** | Música do menu e música do jogo; efeitos de coletar, relógio, dano, ratoeira, dash, miado, tique do tempo, vitória, derrota e clique |
| **2.5 UI** | Pontos, tempo restante, cronômetro e queijos coletados; vidas (corações) e stamina (barra do dash) |
| **2.6 Controles** | Suporte a controle de Xbox/gamepad no jogo e nos menus (Input System), com fallback para o Input Manager antigo |
| **2.7 Level design** | Tema cozinha; arena nova com balcões, montada a partir de um mapa em texto; ratoeiras, relógios e dash como novos desafios |
| **Menu inicial** | Menu com Jogar, Como Jogar e Sair, no mesmo tema do jogo |
| **Sem softlock** | O tempo sempre acaba; pausa com Reiniciar e Menu; tela de fim com Jogar de Novo e Menu; ratoeira desarma depois de fechar; o gato preto não prende o rato contra a parede; se o rato sair da arena, volta ao início |
| **WebGL** | Build WebGL com compressão Gzip e *decompression fallback* (funciona no itch.io) |

## Estrutura do projeto

```
Assets/
  Scripts/    código do jogo (PlayerMovement, GameManager, EnemyPatrol, EnemyChaser, NavGrid, HUD, AudioManager...)
  Editor/     RatoGulosoSetup.cs: monta cenas, prefabs e build (menu "Rato Guloso") e guarda o mapa da fase
  Scenes/     MainMenu, Game, GameOver
  Prefabs/    Queijo, Relogio, Ratoeira, GatoLaranja, GatoPreto
  Art/        sprites
  Resources/Audio/  músicas e efeitos
  Fonts/      fonte Inter (licença OFL)
Tools/
  gerar_assets.py   gera todos os sprites, sons e a capa do itch.io por código
  validar_mapa.py   confere se a fase é jogável (tudo alcançável, patrulhas sem atravessar paredes)
ItchIo/       capa e texto da página do itch.io
```

Para mudar a fase, edite o mapa em texto dentro de `Assets/Editor/RatoGulosoSetup.cs` (entre `MAP_BEGIN` e `MAP_END`) e rode o menu **Rato Guloso > 1. Montar projeto** de novo.

## Créditos e referências

- **Tutorial base:** "Introdução Rápida", Jogos Digitais, Insper (Profs. Tiago Sanches e Gustavo Braga, Tec. Pedro Emil).
- **Arte, músicas e efeitos sonoros:** gerados por código com `Tools/gerar_assets.py` (Python + Pillow + NumPy). Nenhum asset de terceiros.
- **Fonte:** [Inter](https://github.com/rsms/inter), de Rasmus Andersson / The Inter Project Authors, sob a SIL Open Font License 1.1 (`Assets/Fonts/Inter-OFL.txt`).
- **Código:** desenvolvido com auxílio do assistente de IA Claude (Anthropic).
- **Engine:** Unity.
