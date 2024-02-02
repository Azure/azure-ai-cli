local storage = {}

local function execute_command(command)  
    local handle = io.popen(command)  
    local result = handle:read("*a")  
    handle:close()  
    return result  
end  

local function breakLines(input)  
    local iterator = string.gmatch(input, "([^\n]+)")  
    local matches = {}  

    local i = 0  
    for match in iterator do   
        if i == 0 then  
            matches["cert"] = match  
        elseif i == 1 then  
            matches["key"] = match  
        else  
            error("Too many rows returned.")  
        end  
        i = i + 1  
    end  

    return matches  
end  

storage.get_cert = function(self, domain)

    ngx.log(ngx.DEBUG, "Getting cert for domain: " .. domain)
    
    local certFileNames = execute_command("/certs/issue_cert.sh " .. domain)

    if not certFileNames then
        error("Did not get files returned from shell script")
    end

    local parts = breakLines(certFileNames)
    if not (parts["cert"] and parts["key"]) then  
        error("Missing cert or key in the parts table. " .. certFileNames)  
    end  

    local certFile, err = io.open(parts["cert"], "r")
    if not certFile then  
        error("Error opening cert file: " .. parts["cert"] .. " " .. (certErr or "Unknown error"))  
    end  

    local certContent = certFile:read("*a")
    certFile:close()

    local keyFile, err = io.open(parts["key"], "r")
    if not keyFile then  
        error("Error opening key file: " .. parts["key"] .. " " .. (keyErr or "Unknown error"))  
    end  
    local keyContent = keyFile:read("*a")
    keyFile:close()

    local cert = {}

    cert["fullchain_pem"] = certContent
    cert["privkey_pem"] = keyContent

    return cert
end

return storage