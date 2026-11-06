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

Os testes atuais usam imagens sintéticas sem dados pessoais. A captura do monitor, a reprodução do vídeo real, OCR, fila e voz ainda não fazem parte desta fatia.

## Executar as especificações

```powershell
dotnet run --project tests/VerboEmTela.Core.Specs
```

O executável de especificações não usa pacotes externos nem dados de pacientes.

Consulte [plan.md](plan.md) para o plano completo e os critérios de validação.
