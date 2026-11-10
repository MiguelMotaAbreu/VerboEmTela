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
- as demais amostras foram rejeitadas por contagem inesperada de faixas verdes;
- nenhuma amostra foi classificada como painel normal pelo reconhecedor atual.

Duas passagens produziram os mesmos três intervalos e os mesmos limites, exceto pelo primeiro início, observado em 32,4 s e 32,6 s. O manifesto registra a variação máxima de 200 ms: a busca do decodificador serve para localizar estados, mas não deve ser tratada como referência temporal exata de quadro. Após a mudança para consumo incremental, o pico de memória do processo observado externamente foi de 198,5 MiB; essa medição caracteriza a ferramenta neste computador, não o orçamento final da aplicação operacional.

O último item é resultado do algoritmo atual, não prova de que o vídeo não contenha o painel normal. A Etapa 2 permanece incompleta até a causa ser identificada e o reconhecedor distinguir corretamente os períodos intermediários sem criar falsos painéis.
