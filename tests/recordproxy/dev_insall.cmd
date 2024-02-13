docker build -t nginx-recording-test-proxy-dev %~dp0
docker create --name nginx -p 5004:5004 nginx-recording-test-proxy-dev
docker start nginx
curl http://localhost:5004/ca.crt > %TEMP%\ca.crt
certutil -addstore -user -f "root" %TEMP%\ca.crt