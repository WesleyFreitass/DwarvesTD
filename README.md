# 🏰 Dwarves TD (Anões vs Goblins)

Um jogo de **Tower Defense** estratégico de alta performance inspirado nas consagradas mecânicas de *Bloons TD 6*, substituindo macacos e balões por **Anões Defensores** e **Hordas de Goblins**. Desenvolvido em **Unity 6 (6000.x)** com **Universal Render Pipeline (URP 3D)**.

---

## 🎯 Principais Destaques

- **Visão Top-Down Clássica:** Câmera fixa inclinada em 58° ortográfica/perspectiva (enquadramento idêntico ao estilo Monkey Meadow do BTD6).
- **Mecânicas de Alvo BTD6:** Prioridades táticas de mira em tempo real (*Primeiro, Último, Mais Forte, Mais Próximo*).
- **Bestiário de Goblins Híbrido:**
  - 🟢 **Goblin Recruta:** Velocidade e vida equilibradas (Balão Verde).
  - 🟡 **Goblin Corredor:** Altíssima velocidade para furar defesas (Balão Amarelo).
  - 🔘 **Goblin Blindado:** Couraça de ferro que absorve flechas e projéteis leves (Balão de Chumbo).
  - 🟤 **Troll da Caverna (Chefe):** Monstro colossal que, ao ser derrotado, divide-se em 3 recrutas no meio do percurso (*Split on Death* estilo MOAB).
- **Arsenal dos Anões:**
  - 🏹 **Besteiro Anão:** Ataques rápidos e precisos com machados arremessáveis de bronze ($120).
  - 💣 **Canhoneiro de Pólvora:** Bombardeiro de cerco com dano em área (AoE) de 2.5m que estilhaça blindagens ($250).
- **Economia & Ondas:**
  - Bônus de fim de rodada baseado na fórmula oficial: $\text{Ouro} = 100 + (5 \times \text{rodada})$.
  - 10 Rodadas progressivas cuidadosamente curadas.
- **Engenharia de Performance (60–120+ FPS Estáveis):**
  - **Zero Garbage Collection:** Strings cacheadas e reutilização total no HUD.
  - **Zero PhysX Overhead:** Cálculo de colisão puramente matemático para projéteis guiados.
  - **MaterialPropertyBlock:** Variação dinâmica de cores e flash de dano sem clonagem de materiais, preservando GPU Instancing e SRP Batcher.
  - **Forward Rendering URP:** Otimizado para renderização limpa e leve.

---

## 🚀 Como Executar o Projeto

1. Clone o repositório:
   ```bash
   git clone https://github.com/WesleyFreitass/DwarvesTD.git
   ```
2. Abra o **Unity Hub**.
3. Clique em **Add** (Adicionar projeto do disco) e selecione a pasta raiz deste repositório.
4. Abra com **Unity 6 (6000.x)**.
5. No topo do Editor Unity, clique no menu:  
   👉 **`Dwarves TD > Gerar Arena de Teste Jogável`**
6. Dê o **Play ▶️** e defenda a fortaleza!

---

## 🛠️ Tecnologias Utilizadas

- **Engine:** Unity 6 LTS (6000.6.6f1)
- **Render Pipeline:** Universal Render Pipeline (URP 3D)
- **Linguagem:** C# (.NET Standard 2.1)
- **Arquitetura:** Object Pooling genérico, Event-driven architecture, Component-based design.
