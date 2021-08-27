#!/bin/bash
set -euo pipefail

# Change this if you are using your own github repository
gitSource="https://github.com/Azure-Samples/azure-sql-db-named-replica-oltp-scaleout"

# Load values from .env file or create it if it doesn't exists
FILE=".env"
if [[ -f $FILE ]]; then
	echo "loading from .env" 
    export $(egrep . $FILE | xargs -n1)
else
	cat << EOF > .env
ResourceGroup=""
AppName=""
Location=""
ConnectionStrings__AzureSQLConnection="Server=.database.windows.net;Database=;UID=;PWD="
EOF
	echo "Enviroment file not detected."
	echo "Please configure values for your environment in the created .env file"
	echo "and run the script again."
	exit 1
fi

# Make sure connection string variable is set
if [[ -z "${ConnectionStrings__AzureSQLConnection:-}" ]]; then
    echo "ConnectionStrings__AzureSQLConnection."
	exit 1;
fi

echo "Creating Resource Group '$ResourceGroup'...";
az group create \
    -n $ResourceGroup \
    -l $Location

echo "Creating Application Service Plan...";
az appservice plan create \
    -g $ResourceGroup \
    -n "windows-plan" \
    --sku B1     

echo "Creating Web Application '$AppName'...";
az webapp create \
    -g $ResourceGroup \
    -n $AppName \
    --plan "windows-plan" \
    --runtime "DOTNET|5.0" \
    --deployment-local-git \
    --deployment-source-branch main

echo "Configuring Connection String...";
az webapp config connection-string set \
    -g $ResourceGroup \
    -n $AppName \
    --settings AzureSQLConnection=$ConnectionStrings__AzureSQLConnection \
    --connection-string-type=SQLAzure

echo "Getting hostname..."
url=`az webapp show -g $ResourceGroup -n $AppName --query "defaultHostName" -o tsv`

echo "WebApp deployed at: https://$url"

echo "Done."