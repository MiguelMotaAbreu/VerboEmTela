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
