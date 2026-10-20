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

## Créditos e referências

- **Tutorial base:** "Tutorial: Introdução Rápida" da disciplina de Jogos Digitais (Insper).
- **Sprites, fundo, músicas e efeitos sonoros:** criados para este projeto por geração procedural (`Tools/generate_assets.py`). Não usam assets de terceiros.
- **Fontes** (SIL Open Font License 1.1, licenças em `Assets/Fonts/`):
  - [Press Start 2P](https://fonts.google.com/specimen/Press+Start+2P), de CodeMan38
  - [Jersey 10](https://fonts.google.com/specimen/Jersey+10), de Sarah Cadigan-Fried
  - [VT323](https://fonts.google.com/specimen/VT323), de Peter Hull
- **Pacotes Unity:** Universal Render Pipeline (2D), Input System, uGUI.
