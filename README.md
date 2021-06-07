# Notification Hub Poc

É uma api backend web simples para comunicação com o Auzre Notification Hub.

O código foi copiado e adaptado daquele disponível no tutorial da documentação do notification hub.

# Pré-requisitos

- Ter Docker instalado na máquina.
- Ter o Andoid Studio instalado na máquina.

# Execução

Criar um arquivo _.env_ na raiz do projeto e incluir as linhas abaixo com os valores correspondentes ao 
Azure Notification Hub.

  ~~~ ini
  HubName=<hub name>
  HubConStr=<connection string default>
  ~~~

Depois disso executar um o comando abaixo:

  ~~~ shell
  docker-compose up -d 
  ~~~

# Fonte

https://docs.microsoft.com/en-us/azure/notification-hubs/push-notifications-android-specific-users-firebase-cloud-messaging