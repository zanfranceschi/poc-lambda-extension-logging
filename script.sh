#!/bin/bash

rm -rf bin/publish
rm -rf extensions/*

dotnet publish -c Release -f net5.0 -p:Platform=x64 -p:DebugType=None -o bin/publish

rm -rf extensions/appsettings.Development.json

mv bin/publish/* extensions
mv extensions/Poc.LambdaExtension.Logging extensions/poc-lambda-extension-logging

zip -r deploy.zip extensions/*

aws lambda publish-layer-version \
    --region sa-east-1 \
	--layer-name "poc-lambda-extension-logging" \
	--zip-file "fileb://deploy.zip"

aws lambda update-function-configuration \
    --region sa-east-1 \
	--function-name poc-lambda-function-logging --layers $(aws lambda list-layer-versions --layer-name poc-lambda-extension-logging \
	--max-items 1 --no-paginate --query 'LayerVersions[0].LayerVersionArn' \
	--output text)

rm deploy.zip