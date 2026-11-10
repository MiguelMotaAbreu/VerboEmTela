# Componentes de OCR

- `Tesseract` NuGet 5.2.0, wrapper .NET e binários nativos Tesseract 5.2.0. Origem: https://github.com/charlesw/tesseract. Licença: Apache-2.0.
- `por.traineddata` de `tesseract-ocr/tessdata_fast`, commit `87416418657359cb625c412a48b6e1d6d41c29bd`. Origem: https://github.com/tesseract-ocr/tessdata_fast. Licença: Apache-2.0.

Esta integração é uma prova técnica. O wrapper ainda utiliza Tesseract 5.2.0, enquanto o upstream possui versões mais recentes; a decisão de distribuição deverá ser reavaliada antes do pacote candidato.
