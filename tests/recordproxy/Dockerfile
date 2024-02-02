# Use the official OpenResty image as the base image  
FROM openresty/openresty:latest  
  
# Install build dependencies  
RUN apt-get update && \  
    apt-get install -y build-essential libpcre3-dev libssl-dev zlib1g-dev curl dos2unix && \  
    rm -rf /var/lib/apt/lists/* 

RUN mkdir /OpenResty \
    && cd /OpenResty \
    && curl -L https://openresty.org/download/openresty-1.21.4.1.tar.gz | tar xz
    
# Download and compile ngx_http_proxy_connect_module  
RUN cd /OpenResty \
    && curl -L https://github.com/chobits/ngx_http_proxy_connect_module/archive/master.tar.gz | tar xz \
    && curl -L https://raw.githubusercontent.com/chobits/ngx_http_proxy_connect_module/master/patch/proxy_connect_rewrite_102101.patch -o proxy_connect_rewrite_102101.patch \
    && cd openresty-1.21.4.1 \
    && ./configure --add-module=../ngx_http_proxy_connect_module-master --with-debug \
    && patch -d build/nginx-1.21.4/ -p 1 < ../proxy_connect_rewrite_102101.patch \
    && make \
    && make install

RUN apt-get update \
    && apt install -y luarocks

# Install LuaRocks to manage Lua packages  
RUN luarocks install lua-resty-http \
    && luarocks install lua-resty-auto-ssl  \
    && mkdir -p /etc/resty-auto-ssl/storage/file \
    && chown -R www-data:www-data /etc/resty-auto-ssl  \
    && chmod -R 777 /etc/resty-auto-ssl

# Generate our CA OpenSSL Cert & key
RUN mkdir /certs \
    && mkdir /keys \
    && mkdir /logs \
    && mkdir /lua

WORKDIR /certs  

# Create the openssl-ca.conf file  
COPY openssl-ca.conf /certs/openssl-ca.conf
RUN touch index.txt

RUN openssl genrsa -out /keys/ca.key 4096 \
    && openssl req -x509 -new -nodes -key /keys/ca.key -sha256 -days 3650 -subj "/C=US/ST=WA/L=Redmond/O=MSFT/OU=Test" -out ca.crt 
    
#build a default cert for nginx to use
RUN openssl genrsa -out "test.key" 2048 \
    && openssl req -new -key "test.key" -out "test.csr" -subj "/C=US/ST=SomeState/L=SomeCity/O=SomeOrganization/OU=SomeDepartment/CN=azure.example.com" \
    && openssl x509 -req -days 365 -in "test.csr" -CA "ca.crt" -CAkey "/keys/ca.key" -out "test.crt"  -extfile openssl-ca.conf -extensions server_cert_ext

RUN openssl ca -gencrl -config openssl-ca.conf -out empty_crl.pem  

COPY issue_cert.sh /certs/issue_cert.sh

# Set the script file as executable  
RUN dos2unix /certs/issue_cert.sh \
    && chmod +x /certs/issue_cert.sh \
    && chown nobody /keys/ca.key

COPY nginx.conf /usr/local/openresty/nginx/conf/nginx.conf

COPY storage.lua /lua
COPY headers.lua /lua

WORKDIR /