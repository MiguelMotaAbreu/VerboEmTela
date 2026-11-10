# Evidências técnicas

## EV-001 — gravação do painel

Metadados coletados em 9 de novembro de 2026 pelo mecanismo nativo de propriedades do Windows e registrados em `video-evidence.v1.json`.

### Fatos observados

- Tamanho: 3.574.090 bytes.
- Duração: 60 segundos.
- Resolução: 1920x1080.
- Taxa: 60 quadros por segundo.
- Taxa de dados de vídeo informada: 444 kbps.
- Identificador de compactação informado pelo Windows: `{34363248-0000-0010-8000-00AA00389B71}`.
- Taxa de áudio informada: 14 kbps.
- SHA-256: `9A8254E100860649D276AD9498A5C822A2708F004676B715E045449B8981EFCF`.

### Inferências e limites

- A taxa de áudio sustenta que existe uma faixa de áudio, mas o codec não foi identificado pela ferramenta disponível.
- O identificador de compactação foi preservado sem converter ou adivinhar o nome comercial do codec.
- Nenhum quadro, nome, senha, destino ou áudio foi extraído ou versionado.
- O caminho local e o nome original do arquivo não foram registrados no repositório.
- Os estados visuais, a escala efetiva do Windows e a duração do som nativo ainda dependem de validação separada.

### Observações de layout em memória

Em 9 e 10 de novembro de 2026, a gravação identificada pelo mesmo SHA-256 foi decodificada pelo mecanismo multimídia do Windows e amostrada a cada 200 ms, correspondendo aos 5 FPS previstos no plano. Em cada passagem, as 300 amostras foram entregues uma por vez ao reconhecedor; nenhum quadro ou texto foi persistido.

O resumo reproduzível está em `video-layout-evidence.v1.json`:

- três intervalos foram classificados como chamada em tela cheia: aproximadamente 32,4–38,2 s, 42,0–47,4 s e 49,6–55,6 s;
- 206 das 300 amostras foram classificadas como painel normal;
- 89 amostras foram classificadas como chamada em tela cheia;
- cinco amostras de transição permaneceram inválidas, três antes da segunda chamada e duas depois dela.

Duas passagens após o ajuste produziram os mesmos três intervalos principais. Houve variação máxima de 200 ms em limites de transição, por isso a busca do decodificador serve para localizar estados, mas não deve ser tratada como referência temporal exata de quadro. Após a mudança para consumo incremental, o pico de memória do processo observado externamente foi de 198,5 MiB; essa medição caracteriza a ferramenta neste computador, não o orçamento final da aplicação operacional.

O painel real fragmenta algumas faixas verdes com lacunas internas de até 14 pixels. A tolerância foi ajustada para 1,5% da altura, equivalente a 16 pixels em 1080p, sem aproximar as três seções separadas por centenas de pixels. As cinco amostras inválidas não são convertidas em painel por aproximação. Como três delas aparecem imediatamente antes da segunda chamada, a política temporal de transição ainda precisa ser implementada para que essa chamada não seja perdida após um desarme transitório.
