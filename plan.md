# Plano incremental de implementação — VerboEmTela

## 1. Objetivo e condições de sucesso

Construir uma aplicação desktop auxiliar que observe passivamente o painel SoulMV, reconheça cada chamada exibida e anuncie, em português brasileiro, a identificação e o destino correspondentes.

O painel visual continua sendo a fonte operacional principal. A aplicação não controla o atendimento, não altera o SoulMV, não acessa prontuário ou banco de dados e não substitui o fluxo visual existente.

Condições gerais de sucesso:

- Executar localmente no Windows 11 Pro 25H2, x64, sem internet durante o uso.
- Funcionar com o painel aberto pelo Firefox específico ou pelo aplicativo desktop, desde que o layout visual seja equivalente.
- Monitorar Classificação, Cadastro e Atendimento Médico em tela cheia.
- Reconhecer nome ou senha e associá-los ao destino da mesma ocorrência.
- Distinguir tela estática de nova chamada e de rechamada observável.
- Não anunciar leitura incompleta, ambígua ou associada ao destino errado.
- Não persistir nomes, senhas, destinos, quadros da tela ou áudio.
- Não manter logs em disco.
- Preservar o funcionamento e a responsividade do SoulMV.

## 2. Escopo e exclusões

### Incluído

- Captura somente em memória do monitor que contém o painel.
- Reconhecimento do layout em 1920×1080 e escala inicialmente tratada como 150%.
- OCR local das regiões de identificação, senha, setor e destino.
- Detecção de chamada nova, rechamada observável e retorno ao painel normal.
- Validação temporal por múltiplos quadros.
- Frase configurável e mapeamento opcional de destinos.
- Fila FIFO de voz com validade de 15 segundos.
- Síntese de voz neural offline em português brasileiro.
- Inicialização automática com o Windows na sessão interativa.
- Ícone na área de notificação e janela de estado sob demanda.
- Pacotes portáteis versionados, preservação de configuração e rollback.

### Excluído

- Integração com banco, prontuário, API ou tráfego de rede do SoulMV.
- Inspeção de acessibilidade do Flash.
- Controle de chamadas, escrita no SoulMV ou alteração de cadastros.
- Serviço Windows, backend, banco de dados, Docker, nuvem ou LLM.
- Correção generativa, inferência ou complementação de nomes e destinos.
- Captura de áudio do SoulMV para detectar o aviso sonoro.
- Suporte inicial a resoluções diferentes de 1920×1080 ou painel fora de tela cheia.
- Atualização automática pela internet.
- Autenticação própria da aplicação ou bloqueio técnico dos controles da TI.
- Prévia visual de calibração.

## 3. Base técnica e arquitetura

### Stack recomendada

- **Plataforma:** .NET 10 LTS, Windows x64, publicação autocontida.
- **Interface:** WPF, por ser nativa do Windows, madura, leve e adequada à área de notificação.
- **Captura:** Desktop Duplication API/DXGI, encapsulada atrás de uma interface substituível.
- **OCR inicial:** Tesseract 5 com dados de idioma português, empacotado localmente.
- **Voz inicial:** sherpa-onnx com modelo pt-BR Faber medium; outras vozes locais poderão ser comparadas sem alterar a fila ou as regras de chamada.
- **Áudio:** reprodução pelo dispositivo padrão do Windows, com volume e velocidade configuráveis quando suportados pelo motor.
- **Configuração:** arquivo JSON local, sem dados de pacientes, armazenado fora da pasta versionada da aplicação.
- **Testes:** xUnit, relógio e áudio simulados, imagens e sequências exclusivamente sintéticas ou anonimizadas.

Todas as versões, origens, hashes e licenças deverão ser fixados antes da primeira distribuição. O pacote incluirá inventário de componentes e avisos de licença. Assinatura digital não é requisito.

### Componentes lógicos

1. **Capture** — seleciona o monitor, captura quadros e libera a memória imediatamente após o processamento.
2. **Layout Recognition** — confirma que o painel conhecido está visível, em tela cheia e sem obstrução relevante.
3. **Extraction** — recorta as regiões e executa OCR específico para setor, identificação, senha e destino.
4. **Call Detection** — mantém a máquina de estados e cria uma ocorrência apenas diante de transição observável.
5. **Validation** — exige estabilidade temporal, campos coerentes e associação entre identificação e destino.
6. **Utterance Formatting** — normaliza o conteúdo e prepara a frase sem inventar informações.
7. **Queue** — ordena, expira, cancela e entrega anúncios ao motor de voz.
8. **Speech/Audio** — sintetiza localmente e reproduz sem bloquear captura ou interface.
9. **Application State/UI** — expõe estado, contadores em memória, controles e erros atuais.
10. **Configuration/Deployment** — valida configuração, garante instância única, inicialização e rollback.

### Interfaces internas estáveis

As implementações concretas poderão mudar, mas os contratos abaixo devem permanecer separados:

- `ICaptureSource`: produz `CapturedFrame` com pixels, monitor e instante monotônico.
- `ILayoutRecognizer`: classifica o quadro como painel normal, chamada em tela cheia, inválido ou ausente.
- `ITextExtractor`: devolve leituras brutas e limites visuais sem aplicar regras de negócio.
- `ICallDetector`: recebe observações ordenadas e produz candidatos de ocorrência.
- `ICallValidator`: transforma candidato estável em chamada válida ou motivo de rejeição.
- `IUtteranceFormatter`: aplica prioridade nome/senha, pronúncia e mapeamento de destino.
- `IAnnouncementQueue`: mantém FIFO, validade de 15 segundos e cancelamento.
- `ISpeechEngine`: sintetiza PCM localmente sem acessar rede.
- `IAudioOutput`: reproduz, informa disponibilidade e permite interrupção imediata.

Modelos centrais:

- `PanelObservation`: modo visual, setor, identificação, senha, destino, estabilidade e timestamp monotônico.
- `CallCandidate`: sequência de observações pertencentes à mesma transição.
- `ValidatedCall`: ocorrência validada, com identidade escolhida e destino associado.
- `Announcement`: texto preparado, instante de criação, validade e estado de reprodução.

Nenhum desses objetos será serializado. Ao pausar, encerrar, perder o painel ou concluir a reprodução, referências a conteúdo reconhecido serão removidas da memória.

## 4. Regras funcionais fixadas

### Evento de chamada

- A fonte primária é a transição do painel normal para a chamada em tela cheia.
- A área de “Últimas Chamadas” é evidência secundária para ressincronização e testes, não prova universal de unicidade.
- Permanecer na mesma tela cheia não cria outro evento.
- Uma chamada idêntica só cria nova ocorrência após retorno observável ao painel normal e nova entrada em tela cheia.
- Se uma chamada diferente substituir outra sem retorno ao painel, a troca completa e estável de conteúdo poderá criar novo evento; leituras durante a transição serão descartadas.
- Nome mais destino nunca será usado isoladamente como identificador único.

### Estabilidade do OCR

Parâmetros iniciais da prova, mantidos configuráveis internamente:

- Captura a 5 quadros por segundo.
- Pelo menos três observações coerentes em uma janela mínima de 600 ms.
- Setor e destino devem permanecer associados na mesma composição visual.
- Valores conflitantes reiniciam a janela de estabilidade.
- Pontuação do OCR é apenas sinal auxiliar; nunca é garantia de correção.
- Texto tocando o limite do recorte, contendo reticências ou caracteres não permitidos é tratado como incompleto.

### Identificação e frase

- Quando houver nome e senha, anunciar somente o nome.
- Quando houver apenas senha, anunciar cada caractere separadamente.
- Não corrigir, completar ou normalizar nomes por plausibilidade.
- Preservar acentos reconhecidos; unir apenas quebras de linha visuais do mesmo campo.
- Frase padrão: **“{identificação}, dirija-se para {destino}.”**
- Cada ocorrência é anunciada uma vez.
- Mapeamento de destino usa chave normalizada exata.
- Sem mapeamento, usar o texto literal somente depois da validação de estabilidade e caracteres.
- Nome, senha ou destino incompleto bloqueia o anúncio e coloca o aplicativo em aviso atual, sem persistência.

### Som, fila e retomada

- Como o som nativo não será capturado, a voz usará atraso configurável medido a partir do início visual da chamada.
- O atraso inicial será ajustado na prova para que a voz comece depois do som nativo.
- Meta: início da voz até 2 segundos após o fim do som nativo em pelo menos 95% dos eventos medidos.
- A fila é FIFO e cada item expira 15 segundos após sua detecção.
- Se o áudio falhar, tentar recuperar enquanto o item for válido; ao expirar, descartá-lo.
- Ao iniciar, retomar ou recuperar o painel, ignorar o conteúdo já existente e aguardar nova transição.
- Ao perder o painel, pausar anúncios, interromper a voz e limpar a fila.
- Rearmar em até 10 segundos depois de reconhecer o painel válido e estável.

### Estado e interface

Estados visíveis: `Iniciando`, `Armando`, `Monitorando`, `Falando`, `Pausado` e `Erro`.

A janela sob demanda exibirá somente:

- Estado atual e motivo de erro.
- Contadores temporários de eventos aceitos, rejeitados e expirados.
- Monitor selecionado e presença do painel.
- Estado do dispositivo de áudio.
- Pausar, retomar e encerrar.
- Teste de áudio com conteúdo fictício, habilitado apenas no perfil de teste.
- Seleção de voz, velocidade, volume e mapeamentos de destino.

Não haverá histórico de chamadas. Todos os contadores zeram ao encerrar.

## 5. Plano incremental de entregas

Cada etapa só começa após a anterior cumprir seu critério de passagem. Falha em um critério fundamental interrompe a progressão e gera relatório de bloqueio; não deve ser mascarada por ajuste de aceitação.

### Etapa 0 — Congelamento de evidências e matriz de teste

**Objetivo:** preparar material reproduzível sem iniciar ainda o produto operacional.

Entregas:

- Registrar metadados do vídeo recebido e os estados visuais relevantes, sem copiar nomes reais para o repositório.
- Criar especificação de layout com as três faixas, tela normal e tela cheia.
- Criar conjunto sintético de nomes, senhas e destinos, incluindo nomes longos e acentuados.
- Definir matriz de 100 eventos: chamadas, rechamadas, destinos mapeados e literais, senha isolada, rajadas, transições incompletas e telas inválidas.
- Obter evidência anonimizada de chamada somente por senha; enquanto ausente, marcar esse caso como pendência impeditiva do aceite completo.
- Confirmar escala real do monitor e duração aproximada do som nativo.

Testes e aceite:

- Nenhum arquivo de fixture contém identificação real.
- Cada evento sintético possui resultado esperado revisável.
- A matriz cobre as três áreas e todas as regras funcionais.

### Etapa 1 — Esqueleto técnico e contratos

**Objetivo:** criar uma base executável sem OCR nem voz real.

Entregas:

- Solução .NET 10 separando Core, Windows Host, infraestrutura de OCR/voz e testes.
- Máquina de estados da aplicação.
- Relógio monotônico e abstrações de captura, OCR, voz e áudio.
- Instância única por mutex.
- Configuração JSON validada e valores padrão seguros.
- Política de descarte de memória e ausência de logs persistentes.

Testes automatizados:

- Transições válidas e inválidas da máquina de estados.
- Segunda instância recusada.
- Configuração inválida impede monitoramento e informa erro.
- Pausa, erro e encerramento limpam fila e conteúdo em memória.

Critério de passagem:

- Testes unitários aprovados e aplicativo inicia sem rede, banco ou escrita de logs.

### Etapa 2 — Reprodutor de evidências e reconhecedor de layout

**Objetivo:** provar que o painel e seus modos podem ser distinguidos antes do OCR.

Entregas:

- Fonte de captura de teste que reproduz sequências derivadas do vídeo e fixtures sintéticas.
- Reconhecimento de painel normal, chamada em tela cheia, painel ausente, janela errada e obstrução.
- Detecção das três áreas e dos hosts Firefox/aplicativo pela aparência, não pelo nome do processo.
- Seleção automática do único monitor que contém o layout; zero ou múltiplos candidatos geram erro.

Testes automatizados:

- Variações pequenas de brilho e compressão.
- Tela de login, área de trabalho, outra janela e painel parcialmente coberto.
- Painel normal e chamada em cada setor.
- Resolução diferente ou painel fora de tela cheia devem pausar, não tentar adaptar silenciosamente.

Critério de passagem:

- 100% dos quadros de layout da matriz classificados corretamente e zero painel falso.

### Etapa 3 — Prova de OCR local

**Objetivo:** resolver a principal incerteza técnica: leitura correta e associada de identificação e destino.

Entregas:

- Pré-processamento por região, preservando caracteres e acentos.
- Perfis de OCR para nome, senha, setor e destino.
- Normalização determinística e validação de caracteres.
- Resultado por quadro apenas em memória.
- Comparação controlada com alternativa nativa do Windows somente se Tesseract não atingir o critério; a troca ocorre atrás de `ITextExtractor`.

Testes automatizados:

- Nomes curtos, longos, compostos, acentuados e quebrados em linhas.
- Senhas alfanuméricas e zeros iniciais.
- Sala, consultório, recepção, guichê, classificação e odontologia.
- Texto cortado, reticências, ruído, animação e associação cruzada proposital.
- Destino livre sem mapeamento.

Critério de passagem:

- Nos eventos estáveis da matriz: pelo menos 95 de 100 detectados, zero associação errada e zero texto incompleto aceito.
- Se houver qualquer associação errada, a etapa falha independentemente da taxa agregada.
- Se o critério não for atingido, parar e relatar que OCR não está comprovado; não avançar para voz integrada.

### Etapa 4 — Detecção de ocorrência e rechamada

**Objetivo:** transformar observações corretas em eventos únicos.

Entregas:

- Máquina de estados visual `PainelNormal → Transição → ChamadaEstável → Retorno`.
- Supressão de repetição enquanto a mesma chamada permanece na tela.
- Nova emissão após retorno e nova apresentação idêntica.
- Suporte a troca estável para chamada distinta sem retorno, mantendo associação segura.
- Ressincronização sem anunciar conteúdo existente.

Testes automatizados:

- Tela parada por longos períodos.
- Chamada nova e retorno.
- Rechamada idêntica com retorno entre ocorrências.
- Duas chamadas distintas em sequência.
- Mudanças transitórias de OCR que não podem virar evento.
- Pausa, reinício e recuperação com conteúdo antigo visível.

Critério de passagem:

- Todas as rechamadas observáveis da matriz detectadas.
- Zero duplicação sem nova transição e zero anúncio ao armar ou rearmar.

### Etapa 5 — Voz offline isolada

**Objetivo:** validar inteligibilidade e desempenho antes de associar voz a chamadas.

Entregas:

- Integração do sherpa-onnx e primeiro modelo pt-BR.
- Comparação de vozes locais disponíveis com frases exclusivamente fictícias.
- Pronúncia de senha caractere por caractere.
- Ajuste de velocidade e volume.
- Pré-aquecimento do modelo para evitar atraso da primeira fala.
- Reprodução interrompível e detecção de dispositivo indisponível.

Testes automatizados:

- Formação exata da frase.
- Normalização de senha e destino.
- Falha e recuperação simuladas do áudio.
- Cancelamento imediato e descarte após 15 segundos.
- Garantia de que o motor não realiza requisições de rede.

Validação manual no computador de teste:

- A TI escuta nomes e destinos fictícios e escolhe a voz mais clara e natural.
- Se nenhuma for ideal, adotar a melhor voz local, conforme decisão registrada.

Critério de passagem:

- Voz compreensível no computador de teste e síntese rápida o suficiente para a meta integrada.

### Etapa 6 — Integração, fila e interface mínima

**Objetivo:** formar o primeiro protótipo completo, ainda fora da produção.

Entregas:

- Pipeline contínuo de captura, layout, OCR, evento, validação, frase, fila e voz.
- Fila FIFO com validade de 15 segundos e relógio monotônico.
- Atraso configurável para falar depois do som nativo.
- Ícone e janela de estado.
- Inicialização automática na sessão do usuário por atalho na pasta Inicializar; não usar serviço Windows.
- Perfil `Teste`, com áudio fictício, e perfil `Produção`, sem teste de áudio.
- Mutex de instância única.

Testes automatizados:

- Concorrência entre captura e voz.
- Fila cheia, chamadas durante fala, expiração e falha de áudio.
- Perda de painel durante síntese e reprodução.
- Limpeza de memória ao pausar, recuperar ou encerrar.
- Nenhuma gravação de arquivo fora da configuração permitida.

Critério de passagem:

- Matriz integrada de 100 eventos com pelo menos 95 detectados, zero associação errada, zero duplicação indevida e 100% das rechamadas detectáveis.

### Etapa 7 — Soak test e orçamento de recursos

**Objetivo:** comprovar estabilidade no computador da gravação antes de tocar a produção.

Entregas:

- Pacote portátil de teste com componentes e hashes fixados.
- Execução contínua de uma hora com painel estável, obstruções simuladas, perda e recuperação.
- Medição temporária de CPU, memória, latência e fila, exibida em memória e coletada externamente pela TI; a aplicação continua sem logs.

Orçamento técnico inicial:

- CPU média da aplicação até 15% durante o teste; picos maiores são aceitáveis apenas durante OCR/síntese e não podem congelar o SoulMV.
- Memória privada até 1 GB.
- Nenhum crescimento contínuo de memória durante a hora de teste.
- Início da voz até 2 segundos após o fim do som em 95% dos eventos medidos.
- Rearme em até 10 segundos.

Critério de passagem:

- Zero anúncio falso durante uma hora.
- Metas de latência e recuperação cumpridas.
- Nenhuma degradação perceptível do painel.
- Se o orçamento for excedido, otimizar e repetir; não ajustar silenciosamente os limites.

### Etapa 8 — Pacote candidato e documentação

**Objetivo:** preparar uma versão auditável para modo sombra.

Entregas:

- Pasta de release autocontida e imutável.
- Configuração fora da pasta da versão.
- Inventário de dependências, licenças, hashes e versões.
- Manual curto de instalação, início, pausa, erro, atualização e rollback.
- Checklist de privacidade e confirmação de ausência de logs.
- Roteiro de modo sombra com resultados esperados.
- Versão anterior preservada e procedimento de retorno testado.

Critério de passagem:

- Instalação e rollback reproduzidos no computador de teste sem perder configuração.
- Aplicação funciona com rede indisponível.

### Etapa 9 — Modo sombra no computador de produção

**Objetivo:** validar captura e desempenho reais sem emitir voz.

Pré-condições:

- Aprovação de todas as etapas anteriores.
- Acompanhamento presencial da TI.
- Perfil de produção com áudio bloqueado.

Validações:

- Windows 11, resolução, escala, monitor e tela cheia.
- Reconhecimento nos hosts realmente usados.
- Associação visual comparada pela TI com as chamadas observadas.
- Rechamada, quando ocorrer naturalmente.
- CPU, memória, estabilidade e rearmamento.

Critério de passagem:

- Nenhuma associação incorreta observada.
- Taxa de detecção compatível com o mínimo de 95% na amostra acompanhada.
- Nenhum efeito perceptível sobre o SoulMV.
- Qualquer associação incorreta retorna o projeto à Etapa 3; não avançar ao piloto com voz.

### Etapa 10 — Piloto supervisionado com voz

**Objetivo:** realizar a primeira validação acústica real sob controle da TI.

Procedimento:

- Habilitar voz apenas após aprovação formal do modo sombra.
- Acompanhar presencialmente 20 chamadas reais, cobrindo os setores que ocorrerem e pelo menos uma rechamada quando disponível.
- Manter controle de pausa imediatamente acessível.
- Interromper a voz e limpar a fila diante de qualquer divergência de identidade/destino, repetição indevida, atraso inadequado, volume insuficiente, sobreposição ou pronúncia inaceitável.
- Registrar o resultado manualmente fora da aplicação, sem copiar identificação de pacientes.

Critério de passagem:

- Vinte chamadas supervisionadas sem associação incorreta.
- Voz considerada audível e compreensível pela TI.
- Ausência de interferência no atendimento e no painel.
- Se não ocorrer rechamada durante o piloto, manter a rechamada real como pendência, mesmo que validada sinteticamente.

### Etapa 11 — Liberação controlada e manutenção

**Objetivo:** retirar a supervisão contínua sem perder capacidade de retorno.

Entregas:

- Versão aprovada marcada como release operacional.
- Versão anterior mantida para rollback.
- Checklist periódico de painel, áudio, monitor e estado do aplicativo.
- Processo manual de atualização: validar no computador de teste, modo sombra, piloto reduzido e promoção.
- Processo de incidente sem logs: pausar, registrar externamente o horário e o estado exibido, reproduzir com material sintético e retornar à versão anterior se necessário.

## 6. Estratégia de testes

### Testes automatizados obrigatórios

- Unidade: normalização, frase, senha, destino, estabilidade, validade e máquina de estados.
- Sequência: chamada, permanência, retorno, rechamada, rajada, pausa e recuperação.
- Associação: identidade e destino nunca podem vir de quadros ou setores diferentes.
- Fila: FIFO, expiração, falha de áudio, cancelamento e ausência de starvation.
- Privacidade: nenhum modelo central serializável; nenhum log em disco; limpeza em pausa/erro/saída.
- Configuração: validação, migração compatível e preservação no rollback.
- Interface: estados coerentes e nenhuma ação que controle o SoulMV.

### Fixtures

- Somente conteúdo fictício ou anonimizado.
- Nenhum quadro derivado do vídeo real será versionado com identificação visível.
- Cada sequência terá um manifesto com eventos esperados, tempos e resultado de OCR aprovado.
- Variações incluirão compressão, brilho, fonte, quebra de linha, animação e obstrução.

### Critérios globais

- Pelo menos 95 de 100 eventos controlados detectados.
- Zero associação incorreta.
- Zero anúncio falso durante uma hora de soak test.
- Zero duplicação sem nova ocorrência observável.
- Todas as rechamadas detectáveis da amostra reconhecidas.
- Latência de voz dentro da meta em 95% dos eventos medidos.

A taxa de 95% aceita até cinco perdas por 100 chamadas. Por isso, o produto permanece auxiliar e o painel visual não pode ser removido nem desconsiderado.

## 7. Dados, privacidade e segurança

- Capturas existem apenas durante o processamento e são descartadas imediatamente.
- Texto reconhecido permanece apenas no candidato, na chamada validada e na fila vigente.
- A fila é apagada ao pausar, perder o painel, encerrar ou expirar.
- A configuração armazena somente geometria, monitor, voz, volume, velocidade, atraso e mapeamentos de destino.
- Não armazenar nomes, senhas, destinos observados, screenshots, áudio ou histórico.
- Não emitir telemetria nem realizar chamadas de rede em execução.
- Não adicionar modo diagnóstico persistente sem nova decisão explícita.
- Controles destinados à TI não têm autenticação; a restrição é operacional e deve constar na documentação.

## 8. Distribuição, atualização e rollback

Estrutura recomendada no computador-alvo:

- Uma pasta por release, sem modificação após a instalação.
- Configuração compartilhada fora das pastas de release.
- Atalho de inicialização apontando para a release ativa.
- Atualização instala nova pasta, valida configuração, executa teste de abertura e só então muda o atalho.
- Rollback reponta o atalho para a versão anterior e reutiliza a configuração compatível.
- Antes de qualquer migração incompatível, criar backup somente da configuração não sensível.

O pacote deve conter:

- Executáveis e runtimes necessários.
- Modelo de OCR e voz fixados por hash.
- Arquivo de configuração padrão.
- Licenças e atribuições.
- Inventário de componentes/SBOM.
- Instruções de instalação, operação, atualização e rollback.

## 9. Pendências que bloqueiam o aceite completo

- Evidência anonimizada de chamada somente por senha.
- Confirmação da escala de 150% no monitor de produção.
- Medição do som nativo para ajustar o atraso da voz.
- Prova de OCR no vídeo e na matriz sintética.
- Seleção auditiva da melhor voz local.
- Soak test no computador da gravação.
- Modo sombra no computador real.
- Primeiro teste acústico real durante o piloto de 20 chamadas.
- Rechamada real no piloto, caso não ocorra durante a janela acompanhada.

Nenhuma etapa deve declarar o produto “validado para produção” antes da conclusão do modo sombra e do piloto supervisionado.
