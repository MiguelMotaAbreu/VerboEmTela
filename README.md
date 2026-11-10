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

Faixas verdes fragmentadas por pequenas lacunas internas são reunidas antes da contagem. A tolerância padrão é de 1,5% da altura — 16 pixels em 1080p — validada contra a gravação fornecida; as separações reais entre as três áreas continuam muito maiores e não são fundidas.

Os testes atuais usam imagens sintéticas sem dados pessoais. A reprodução do vídeo real, OCR, fila e voz ainda não fazem parte desta fatia.

O processador de quadros conecta essas duas partes e garante que:

- texto só seja extraído quando o layout for uma chamada em tela cheia;
- conteúdo já visível ao iniciar não seja enviado ao extrator;
- nome tenha prioridade sobre senha;
- senha seja preservada como tipo próprio quando não houver nome;
- setor, identidade e destino venham da mesma leitura;
- leitura incompleta não produza ocorrência nem desarme uma chamada potencialmente válida;
- layout inválido desarme o pipeline até um novo painel normal.

Para não perder uma chamada durante a animação observada na gravação, o primeiro quadro inválido abre uma transição de até 800 ms. Durante essa janela não há OCR nem aceitação de conteúdo. Se painel ou chamada válidos retornarem antes do limite, o estado armado é preservado; se a invalidez persistir ou o próximo quadro chegar depois do limite, a perda é confirmada, o detector desarma e volta a exigir painel normal.

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
- aceita `P` para pausar, `R` para retomar e `Q` para encerrar, sempre seguidos de `Enter`; `Ctrl+C` continua disponível.

O estado da aplicação é controlado separadamente do estado visual do painel:

- inicia em `Iniciando` e passa a `Armando` depois da configuração e seleção do monitor;
- só entra em `Monitorando` após observar o painel normal;
- uma chamada já visível durante a inicialização não conclui o armamento;
- perder o painel durante o monitoramento retorna a `Armando` e limpa conteúdo transitório;
- a pausa interrompe novas capturas; a retomada volta a `Armando` antes de liberar a captura;
- pausa, retomada, erro, recuperação e encerramento também exigem limpeza;
- transições incoerentes são recusadas pela máquina de estados.

Este executável é uma ferramenta de diagnóstico, não uma versão operacional para atendimento. A primeira execução no computador da unidade ainda precisa ser acompanhada e validada.

### Configuração externa opcional

Sem argumentos, o diagnóstico usa os padrões seguros de 1920x1080 e 5 FPS. Um arquivo externo pode ser informado sem modificar a pasta da aplicação:

```powershell
dotnet run --project src/VerboEmTela.Diagnostic -- --config C:\VerboEmTelaConfig\monitoring.json
```

Formato aceito nesta versão:

```json
{
  "schemaVersion": 1,
  "requiredWidth": 1920,
  "requiredHeight": 1080,
  "framesPerSecond": 5
}
```

- Campos omitidos recebem os valores seguros acima.
- A frequência pode variar de 5 a 10 FPS.
- Outra versão, resolução diferente, JSON inválido ou campo desconhecido impede o início da captura.
- O programa apenas lê o arquivo: não cria, modifica ou migra configurações automaticamente.
- O arquivo contém somente parâmetros técnicos e deve permanecer fora das pastas de release.

## Executar as especificações

```powershell
dotnet run --project tests/VerboEmTela.Core.Specs
```

O executável de especificações não usa pacotes externos nem dados de pacientes.

### Matriz sintética

As especificações mantêm uma matriz determinística de 100 casos exclusivamente fictícios. Ela contém dez casos de cada categoria:

- chamada por nome e somente por senha;
- rechamada após retorno ao painel e chamada diferente sem retorno intermediário;
- destino abreviado com mapeamento e destino literal;
- nome longo e nome acentuado;
- transição incompleta e tela inválida.

Todos os nomes começam com `PESSOA FICTÍCIA` e todas as senhas usam o prefixo reservado `TST`. A matriz cobre recepção, classificação e odontologia, possui resultado esperado e é executada integralmente pelos testes. Ela ainda não substitui as fixtures visuais necessárias para comprovar OCR.

### Evidência do vídeo

Os metadados técnicos e o hash da gravação fornecida estão em [evidence/README.md](evidence/README.md) e [evidence/video-evidence.v1.json](evidence/video-evidence.v1.json). O manifesto é validado pelas especificações e contém apenas duração, dimensões, taxas, identificadores técnicos, tamanho e hash. Nenhum quadro, áudio ou conteúdo do painel foi incluído no repositório.

O analisador de evidência pode amostrar um vídeo local usando o decodificador multimídia do Windows e aplicar o reconhecedor de layout aos quadros mantidos somente em memória:

```powershell
dotnet run --project src/VerboEmTela.EvidenceProbe -- <video.mp4> 500
```

O segundo argumento é o intervalo entre amostras em milissegundos, de 100 a 5000. Cada quadro é classificado e liberado antes da próxima amostra. A saída contém apenas intervalos de estado visual e motivos técnicos de rejeição; o utilitário não executa OCR, não reproduz áudio e não grava quadros ou textos.

### Regiões de imagem

O núcleo permite recortar uma região validada de um quadro RGB24 inteiramente em memória. Regiões vazias, negativas, com estouro numérico ou fora dos limites são rejeitadas. Cada recorte recebe um buffer independente e expõe somente memória de leitura, preparando setor, identidade e destino para pré-processamentos separados sem definir coordenadas do SoulMV antes da validação visual.

Um extrator de regiões agrupa setor, nome, senha e destino a partir do mesmo `CapturedFrame`, preservando um único timestamp. O layout é validado integralmente antes dos recortes; se qualquer região estiver fora do quadro, o lote inteiro é rejeitado. Isso cria uma fronteira explícita contra a associação de identidade e destino provenientes de quadros diferentes.

Os quatro recortes podem ser preparados conjuntamente como luminância de 8 bits, reduzindo cada pixel RGB de três bytes para um byte e preservando dimensões, campo e timestamp. A conversão usa pesos inteiros fixos e não aplica limiar, ampliação, nitidez ou correção de conteúdo; esses ajustes só serão escolhidos depois da comparação com fixtures visuais.

Um contrato de OCR por região recebe explicitamente o tipo do campo e somente seu quadro de luminância. O extrator coordenador lê setor, nome, senha e destino do mesmo `CapturedFrame` e só então devolve o conjunto de textos brutos. A confiança opcional é validada entre zero e um, mas não é tratada como garantia nem usada para completar conteúdo. O motor concreto de OCR ainda não foi escolhido.

Antes das regras de chamada, o texto bruto passa por normalização Unicode Form C e espaços ou quebras de linha consecutivos viram um único espaço. Acentos, caixa e pontuação válida são preservados. Leituras vazias, com reticências, caracteres de controle/invisíveis, caractere de substituição ou texto tocando a borda do recorte são convertidas em campo ausente; nenhuma tentativa de completar ou corrigir o conteúdo é feita.

A validação final considera o tipo do campo: senha aceita somente caracteres alfanuméricos, enquanto setor, nome e destino preservam pontuação textual. Categorias Unicode de símbolos, como emoji, moeda e operadores matemáticos, são rejeitadas em todos os campos. Uma leitura rejeitada sai do extrator como campo ausente e, portanto, não pode formar uma ocorrência.

Consulte [plan.md](plan.md) para o plano completo e os critérios de validação.
