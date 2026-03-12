Feature: Gerenciamento de Tarefas
  Como um usuário autenticado do sistema UpTask
  Quero criar, atribuir e gerenciar tarefas dentro de projetos
  Para que eu possa organizar o trabalho da minha equipe com eficiência

  Background:
    Given que o usuário "Ana Silva" com email "ana@uptask.com" está autenticado
    And existe um projeto chamado "Sprint 2025-Q1" com prioridade Alta

  Scenario: Criar uma tarefa avulsa com sucesso
    Given que o usuário é membro do projeto
    When o usuário cria uma tarefa com os seguintes dados:
      | Campo          | Valor                        |
      | Título         | Implementar autenticação JWT |
      | Prioridade     | Alta                         |
      | StoryPoints    | 5                            |
      | HorasEstimadas | 8                            |
    Then a tarefa deve ser criada com status "Pendente"
    And o campo "CreatedBy" deve ser o usuário atual
    And um evento de domínio "TaskCreated" deve ter sido emitido

  Scenario: Falha ao criar tarefa com título vazio
    When o usuário tenta criar uma tarefa com título vazio
    Then a operação deve falhar com o erro "Task title is required."
    And nenhuma tarefa deve ser persistida

  Scenario: Falha ao criar tarefa com título excedendo 250 caracteres
    When o usuário tenta criar uma tarefa com título de 251 caracteres
    Then a operação deve falhar com o erro de validação de comprimento máximo

  Scenario: Falha ao criar tarefa em projeto sem ser membro
    Given que o usuário NÃO é membro do projeto
    When o usuário tenta criar uma tarefa no projeto
    Then a operação deve retornar o erro "Unauthorized"

  Scenario: Criar tarefa com prazo futuro válido
    Given que o usuário é membro do projeto
    When o usuário cria uma tarefa com prazo para daqui 7 dias
    Then a tarefa deve ser criada com o prazo correto
    And a tarefa não deve estar marcada como atrasada

  Scenario: Atribuir tarefa a um membro do projeto com sucesso
    Given existe uma tarefa "Revisar pull request #42" no projeto
    And a tarefa está atribuída ao usuário atual
    When o dono da tarefa atribui a tarefa para "Carlos Lima"
    Then a tarefa deve ter o responsável "Carlos Lima"
    And um evento de domínio "TaskAssigned" deve ter sido emitido

  Scenario: Falha ao atribuir tarefa a usuário fora do projeto
    Given existe uma tarefa "Corrigir bug #99" no projeto
    When o dono da tarefa tenta atribuir a tarefa para "Maria Souza"
    Then a operação deve falhar com o erro de regra de negócio "Assignee must be a project member."

  Scenario: Concluir uma tarefa pendente com sucesso
    Given existe uma tarefa "Escrever testes unitários" no projeto
    And a tarefa está atribuída ao usuário atual
    When o usuário conclui a tarefa
    Then a tarefa deve ter o status "Concluída"
    And o campo "CompletedAt" deve estar preenchido
    And um evento de domínio "TaskCompleted" deve ter sido emitido

  Scenario: Falha ao concluir uma tarefa já concluída
    Given existe uma tarefa concluída "Deploy em produção"
    When o usuário tenta concluir a tarefa novamente
    Then a operação deve falhar com o erro "Task is already completed."

  Scenario: Falha ao concluir uma tarefa cancelada
    Given existe uma tarefa cancelada "Migrar banco de dados"
    When o usuário tenta concluir a tarefa
    Then a operação deve falhar com o erro "Cannot complete a cancelled task."

  Scenario: Tarefa com prazo expirado e pendente deve ser marcada como atrasada
    Given existe uma tarefa com prazo de ontem
    And a tarefa está com status "Pendente"
    When o sistema verifica o status de atraso da tarefa
    Then a propriedade "IsOverdue" deve ser verdadeira

  Scenario: Projeto deve ser automaticamente concluído ao atingir 100 porcento de progresso
    Given o projeto está com status "Ativo"
    And o projeto tem 4 tarefas no total e 3 já concluídas
    When o usuário conclui a última tarefa do projeto
    Then o progresso do projeto deve ser 100%
    And o projeto deve ter o status "Concluído"
    And o campo "ActualEndDate" do projeto deve estar preenchido
    And um evento de domínio "ProjectCompleted" deve ter sido emitido

  Scenario: Progresso do projeto com zero tarefas deve ser zero
    Given o projeto está com status "Ativo"
    And o projeto não possui tarefas cadastradas
    When o sistema recalcula o progresso
    Then o progresso do projeto deve ser 0%

  Scenario: Adicionar novo membro ao projeto com sucesso
    Given que o usuário atual é admin do projeto
    And existe um usuário "Pedro Costa" cadastrado no sistema
    When o admin adiciona "Pedro Costa" ao projeto com papel de "Colaborador"
    Then "Pedro Costa" deve estar na lista de membros do projeto
    And um evento de domínio "ProjectMemberAdded" deve ter sido emitido

  Scenario: Falha ao remover o dono do projeto
    Given que o usuário atual é admin do projeto
    When o admin tenta remover o dono do projeto
    Then a operação deve falhar com o erro "Cannot remove the project owner."
