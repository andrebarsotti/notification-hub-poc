# Notification Hub Poc

É uma api backend web simples para comunicação com o Auzre Notification Hub.

O código foi copiado e adaptado daquele disponível no tutorial da documentação do notification hub.

# Pré-requisitos

- Ter Docker instalado na máquina.
- Ter o Andoid Studio instaldo na máquina.

# Execução

Editar o arquivo _docker-compose.yml_ e alterar as variáveis de ambiente abaixo para os valores correspondentes ao 
Azure Notification Hub.

  ~~~ yaml
        - AzureNotificationHub__ConnectionString=<connection string>
        - AzureNotificationHub__HubName=<hub name>
  ~~~

Depois disso executar um o comando abaixo:

  ~~~ bash
  docker-compose up -d 
  ~~~

# Fonte

https://docs.microsoft.com/en-us/azure/notification-hubs/push-notifications-android-specific-users-firebase-cloud-messaging