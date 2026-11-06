# VerboEmTela

Aplicação auxiliar de chamadas por voz para o painel SoulMV.

## Fatia implementada

O núcleo recebe observações visuais já interpretadas e detecta limites de ocorrência:

- ignora chamada que já estava visível ao iniciar;
- arma somente depois de observar o painel normal;
- exige três leituras coerentes durante pelo menos 600 ms;
- emite uma única ocorrência enquanto a tela permanece igual;
- aceita uma chamada idêntica como rechamada depois do retorno ao painel;
- desarma quando o painel fica inválido;
- aceita uma chamada diferente que substitua outra após estabilização.

O reconhecedor estrutural também recebe um quadro RGB24 em memória e classifica:

- painel normal com três faixas de seção;
- chamada em tela cheia com uma faixa de seção;
- tela inválida por dimensão, estrutura incompleta, banner estreito ou obstrução relevante.

Os testes atuais usam imagens sintéticas sem dados pessoais. A reprodução do vídeo real, OCR, fila e voz ainda não fazem parte desta fatia.

O processador de quadros conecta essas duas partes e garante que:

- texto só seja extraído quando o layout for uma chamada em tela cheia;
- conteúdo já visível ao iniciar não seja enviado ao extrator;
- nome tenha prioridade sobre senha;
- senha seja preservada como tipo próprio quando não houver nome;
- setor, identidade e destino venham da mesma leitura;
- leitura incompleta não produza ocorrência nem desarme uma chamada potencialmente válida;
- layout inválido desarme o pipeline até um novo painel normal.

A fonte de sequência e o executor assíncrono permitem reproduzir quadros temporizados sem espera artificial:

- a sequência é validada antes da execução e deve estar em ordem cronológica;
- cada quadro produz um resultado na mesma ordem;
- somente uma execução pode usar o mesmo executor por vez;
- conclusão, cancelamento ou falha limpam o estado do processador;
- o tempo vem do quadro, preparando a troca futura pela captura real.

O adaptador Windows adiciona uma primeira captura real de região de tela:

- recebe coordenadas explícitas, inclusive negativas para monitores à esquerda;
- captura no ritmo configurado, com padrão de 5 FPS;
- converte o bitmap BGR do Windows para RGB24 em memória;
- não grava imagens nem mantém histórico de quadros;
- usa GDI/`BitBlt` como prova mínima sem dependências externas.

A seleção inicial de monitor também é baseada na aparência do painel:

- enumera os monitores informados pelo Windows, incluindo posição e dimensão;
- captura um único quadro de cada monitor e aplica o reconhecedor de layout;
- seleciona somente quando exatamente um monitor contém um painel válido;
- zero ou múltiplos candidatos retornam estados de erro, sem escolha silenciosa;
- não retém os quadros usados durante a seleção.

O adaptador GDI é provisório. A versão candidata à produção continuará prevendo Desktop Duplication/DXGI, atrás do mesmo `IFrameSource`, após comparação de desempenho e confiabilidade.

## Executar o diagnóstico visual

O diagnóstico conecta seleção de monitor, captura e reconhecimento de layout, mas não executa OCR nem voz:

```powershell
dotnet run --project src/VerboEmTela.Diagnostic
```

Antes de iniciar, mantenha o painel em tela cheia no monitor de 1920x1080 e deixe o terminal no outro monitor. O programa:

- encerra se encontrar zero ou mais de um painel válido;
- captura a 5 FPS e imprime apenas mudanças entre painel normal, chamada em tela cheia e painel inválido;
- não mostra textos do painel, não grava imagens e não cria logs;
- recusa uma segunda instância na mesma sessão do Windows antes de capturar a tela;
- encerra com `Ctrl+C`.

Este executável é uma ferramenta de diagnóstico, não uma versão operacional para atendimento. A primeira execução no computador da unidade ainda precisa ser acompanhada e validada.

## Executar as especificações

```powershell
dotnet run --project tests/VerboEmTela.Core.Specs
```

O executável de especificações não usa pacotes externos nem dados de pacientes.

Consulte [plan.md](plan.md) para o plano completo e os critérios de validação.
