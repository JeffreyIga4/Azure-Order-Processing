param workflows_logicapp_scheduled_check_name string = 'logicapp-scheduled-check'
param healthCheckUrlBase string

@secure()
param healthCheckKeySecretId string

resource workflows_logicapp_scheduled_check_name_resource 'Microsoft.Logic/workflows@2017-07-01' = {
  name: workflows_logicapp_scheduled_check_name
  location: 'northeurope'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    state: 'Enabled'
    definition: {
      '$schema': 'https://schema.management.azure.com/providers/Microsoft.Logic/schemas/2016-06-01/workflowdefinition.json#'
      contentVersion: '1.0.0.0'
      parameters: {
        '$connections': {
          defaultValue: {}
          type: 'Object'
        }
      }
      triggers: {
        Recurrence: {
          recurrence: {
            interval: 5
            frequency: 'Minute'
            timeZone: 'GMT Standard Time'
          }
          evaluatedRecurrence: {
            interval: 5
            frequency: 'Minute'
            timeZone: 'GMT Standard Time'
          }
          type: 'Recurrence'
        }
      }
      actions: {
        GetSecret: {
          type: 'AzureKeyVault.GetSecret'
          inputs: {
            host: {
              connectionName: '@parameters("$connections")["azurekeyvault"]["connectionId"]'
              operationId: 'GetSecret'
              apiId: '/providers/Microsoft.PowerApps/apis/azurekeyvault'
            }
            parameters: {
              secretIdentifier: healthCheckKeySecretId
            }
          }
          runAfter: {}
        }
        HTTP: {
          runAfter: {
            GetSecret: ['Succeeded']
          }
          type: 'Http'
          inputs: {
            uri: '${healthCheckUrlBase}?code=@{actions("GetSecret").outputs.body.value}'
          method: 'GET'
          }
          runtimeConfiguration: {
            contentTransfer: {
              transferMode: 'Chunked'
            }
          }
        }
        Condition: {
          actions: {
            Compose: {
              type: 'Compose'
              inputs: 'Service Healthy'
            }
          }
          runAfter: {
            HTTP: [
              'Succeeded'
              'Failed'
              'TimedOut'
              'Skipped'
            ]
          }
          else: {
            actions: {
              Compose_1: {
                type: 'Compose'
                inputs: 'Health check failed at @{utcNow()} – status not 200'
              }
            }
          }
          expression: {
            and: [
              {
                equals: [
                  '@outputs(\'HTTP\')[\'statusCode\']\n'
                  200
                ]
              }
            ]
          }
          type: 'If'
        }
      }
      outputs: {}
    }
    parameters: {
      '$connections': {
        value: {}
      }
    }
  }
}
