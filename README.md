# Plugin Furo Automático para Revit

Este plugin para Revit automatiza a criação de furos em lajes nos pontos de interseção com tubulações, vinculando modelos MEP e estruturais, detectando colisões e inserindo famílias de abertura predefinidas.

## Como usar
1. Salve seu modelo hospedeiro na mesma pasta dos arquivos RVT (tubos e estrutural)
2. Clique em **"Executar Plugin"** na aba **"Furos Automáticos"** do ribbon do Revit
3. Selecione os modelos:
   - Modelo MEP (tubulações)
   - Modelo Estrutural (lajes)
4. Escolha uma ação:
   - **Executar**: Processar interseções
   - **Cancelar**: Remover vínculos temporários e fechar

## Estrutura do Projeto
```
/FuroAutomaticoRevit
├── 1_App/             # Inicialização do plugin
├── 2_Commands/        # Implementações dos comandos do Revit
├── 3_UI/              # Interface WPF (padrão MVVM)
│   ├── Utils/         # Utilitários de gerenciamento de janelas
│   ├── ViewModels/    # Lógica de negócios
│   ├── Views/         # Componentes visuais
│   └── Resources/     # Estilos e recursos visuais
├── 4_Core/            # Serviços de lógica central
├── 5_Revit/           # Serviços específicos da API do Revit
└── 6_Domain/          # Objetos de transferência de dados (DTOs)
```

## Roteiro de Desenvolvimento

### Tarefas Concluídas 
- [x] Integração do plugin ao ribbon
- [x] Interface WPF com arquitetura MVVM
- [x] Carregamento dinâmico de modelos a partir do diretório do projeto
- [x] Vinculação de modelos
- [x] Gerenciamento de visibilidade dos links
- [x] Limpeza de vínculos temporários por sessão
- [x] Manutenção da janela em primeiro plano
- [x] Persistência de seleção nas ComboBoxes

### Tarefas Pendentes 
- [ ] Serviço de detecção de interseções
- [ ] Criação de furos com a família `FURO-QUADRADO-LAJE`
- [ ] Cálculo de dimensões (espessura da laje + 5 cm)
- [ ] Filtro de elementos por vista ("Vista Teste")
- [ ] Criação do pacote de instalação
- [ ] Documentação para o usuário