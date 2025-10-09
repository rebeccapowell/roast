#!/bin/bash

docker build -t coffeetalk-web:latest -t lbhackweekroast2025.azurecr.io/coffeetalk-web:latest .
docker push lbhackweekroast2025.azurecr.io/coffeetalk-web:latest
