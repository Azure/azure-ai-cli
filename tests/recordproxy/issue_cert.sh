#!/bin/bash  
  
# Check if the required argument is provided  
if [ -z "$1" ]; then  
  echo "Usage: $0 <domain>"  
  exit 1  
fi  
  
DOMAIN=$1  
CA_CERT="/certs/ca.crt"  
CA_KEY="/keys/ca.key"  
CA_SERIAL="./ca.srl"  
  
# Check if required files exist  
if [ ! -f "${CA_CERT}" ] || [ ! -f "${CA_KEY}" ]; then  
  echo "Error: CA certificate or key file not found."  
  exit 1  
fi  
  
CERT_DIR="/etc/resty-auto-ssl/certs"  
mkdir -p "${CERT_DIR}/${DOMAIN}"  
  
KEY_FILE="${CERT_DIR}/${DOMAIN}/server.key"  
CSR_FILE="${CERT_DIR}/${DOMAIN}/request.csr"  
CERT_FILE="${CERT_DIR}/${DOMAIN}/fullchain.pem"  
  
# Generate private key  
openssl genrsa -out "${KEY_FILE}" 2048  2>> /tmp/issue_cert.log
if [ $? -ne 0 ]; then  
  echo "Error: Private key generation failed."  
  cat /tmp/issue_cert.log
  exit 1  
fi  
  
# Generate CSR  
openssl req -new -key "${KEY_FILE}" -out "${CSR_FILE}" -subj "/C=US/ST=SomeState/L=SomeCity/O=SomeOrganization/OU=SomeDepartment/CN=${DOMAIN}"  2>> /tmp/issue_cert.log
if [ $? -ne 0 ]; then  
  echo "Error: CSR generation failed."  
  cat /tmp/issue_cert.log
  exit 1  
fi  
  
# Sign the certificate with your internal CA  
openssl x509 -req -days 365 -in "${CSR_FILE}" -CA "${CA_CERT}" -CAkey "${CA_KEY}" -out "${CERT_FILE}"  -extfile <(printf "basicConstraints=CA:FALSE\nkeyUsage=nonRepudiation,digitalSignature,keyEncipherment\ncrlDistributionPoints=URI:http://localhost:5004/empty_crl.pem")  2>> /tmp/issue_cert.log
if [ $? -ne 0 ]; then  
  echo "Error: Certificate signing failed."  
  cat /tmp/issue_cert.log
  exit 1  
fi  
  
# Output the paths  
echo "${CERT_FILE}"  
echo "${KEY_FILE}"  