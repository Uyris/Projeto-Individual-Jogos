# Coletor Estelar

Projeto individual de **Introdução ao Unity**: o tutorial "Introdução Rápida" (jogador, física, coletáveis, prefabs, áudio, movimentação diagonal, menu inicial e GameManager) expandido para um jogo completo com tema espacial.

> Você é um astronauta preso em uma estação espacial abandonada. Colete os **12 cristais de energia** para liberar a **cápsula de fuga** e escape antes que o **oxigênio** acabe, desviando de drones, alienígenas, minas e lasers.

- **Jogar no navegador (itch.io):** _adicionar link_
- **Autor:** Yuri Henrique
- **Engine:** Unity 6.3 LTS (6000.3.25f1), URP 2D, Input System

## Como jogar

| Ação | Teclado | Controle (Xbox / gamepad) |
|---|---|---|
| Mover | WASD / setas | Analógico esquerdo / D-pad |
| Dash (gasta stamina, invulnerável por um instante) | Espaço / Shift | A / RT |
| Pausar | Esc / P | Start |
| Navegar nos menus | Setas + Enter / mouse | D-pad + A (B volta) |

- **Cristal:** +100 pontos e +3 s de oxigênio.
- **Tanque de O2:** +10 s de oxigênio.
- O oxigênio cai 1 s por segundo. Se chegar a zero, ou se você levar 3 golpes, a partida acaba.
- Depois do último cristal, a cápsula (canto superior direito) abre. Entre nela para vencer.
- No final aparecem o **tempo final**, a pontuação e os bônus (oxigênio restante e vidas). O melhor tempo fica salvo.

## O que foi implementado (rubrica)

| Item | Como aparece no jogo |
|---|---|
| **Tutorial** | Jogador com `Rigidbody2D` + `CircleCollider2D`, paredes com `BoxCollider2D`, script de movimento no `FixedUpdate`, coletáveis triangulares com `PolygonCollider2D` (trigger) e a tag `Coletavel`, prefabs, som de coleta no `AudioSource` do jogador, correção da diagonal, menu inicial e `GameManager` estático com fim de jogo. |
| **Mecânica de tempo** | Cronômetro na HUD, tempo final na vitória e na derrota, oxigênio como tempo limite (os itens dão mais tempo), lasers que ligam e desligam em ciclos e melhor tempo salvo com `PlayerPrefs`. |
| **Inimigos** | Drones que patrulham rotas fixas (vai-e-volta ou circuito), minas paradas, lasers temporizados e alienígenas com comportamento variável: **patrulha → perseguição** (raio de visão + linha de visada) **→ busca** na última posição vista. |
| **Visual** | Sprites básicos com a cor trocada (lasers, zonas do piso, sombras) e sprites pixel art próprios usados de forma coerente com o tema. Luzes 2D (URP), bloom, vinheta, partículas e rastro do dash. |
| **Áudio** | Música no menu e na partida, além de efeitos de coleta, oxigênio, dano, dash, alerta do alienígena, alarme de oxigênio baixo, contagem regressiva, cápsula liberada, vitória, derrota e cliques/seleção na interface. |
| **UI** | HUD com pontuação, cronômetro, cristais, corações (vida), barra de oxigênio e barra de stamina. Também tem mensagens na tela e painéis de pausa, derrota e vitória. |
| **Controles** | Suporte a controle Xbox/gamepad pelo Input System, tanto no jogo quanto nos menus. |
| **Level design** | Tema de estação espacial com uma arena nova (salas, corredores, zona inicial segura e sala da cápsula protegida por laser). Desafios novos: oxigênio limitado, lasers com ritmo, cápsula trancada e dash com stamina. |
| **Sem softlock** | Pausa com "Reiniciar" e "Menu principal" disponível a qualquer momento. O jogador nunca fica preso: inimigos não bloqueiam a passagem, há invulnerabilidade após dano e as telas de fim de jogo sempre oferecem reiniciar ou voltar ao menu. |

## Estrutura do projeto

```
Assets/
  Scenes/      MainMenu.unity, Game.unity
  Scripts/     PlayerController, GameManager, HUD, ChaserEnemy, PatrolEnemy, LaserGate, ...
  Prefabs/     Player, Cristal, TanqueOxigenio, Drone, Alienigena, Mina, CapsulaDeFuga, FX
  Art/         sprites, UI e fundo (gerados por código)
  Audio/       músicas e efeitos (gerados por código)
  Fonts/       fontes pixel (licença OFL)
  Editor/      GameBuilder.cs: monta prefabs, cenas e configurações (menu "Coletor Estelar")
Tools/
  generate_assets.py: gera todos os sprites e sons (Python + numpy + Pillow)
```

Para regenerar tudo: `python Tools/generate_assets.py` e, no Unity, use o menu **Coletor Estelar > Reconstruir Jogo**. O build WebGL sai pelo menu **Coletor Estelar > Gerar Build WebGL** (pasta `Builds/WebGL`).

## Créditos e referências

- **Tutorial base:** "Tutorial: Introdução Rápida" da disciplina de Jogos Digitais (Insper).
- **Sprites, fundo, músicas e efeitos sonoros:** criados para este projeto por geração procedural (`Tools/generate_assets.py`). Não usam assets de terceiros.
- **Fontes** (SIL Open Font License 1.1, licenças em `Assets/Fonts/`):
  - [Press Start 2P](https://fonts.google.com/specimen/Press+Start+2P), de CodeMan38
  - [Jersey 10](https://fonts.google.com/specimen/Jersey+10), de Sarah Cadigan-Fried
  - [VT323](https://fonts.google.com/specimen/VT323), de Peter Hull
- **Pacotes Unity:** Universal Render Pipeline (2D), Input System, uGUI.
