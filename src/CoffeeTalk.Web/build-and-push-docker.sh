#!/bin/bash

docker build -t coffeetalk-web:latest -t lbhackweekroast2025.azurecr.io/coffeetalk-web:latest --platform linux/amd64 .
docker push lbhackweekroast2025.azurecr.io/coffeetalk-web:latest
