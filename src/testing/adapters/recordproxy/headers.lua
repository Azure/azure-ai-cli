local function start(self, mode) 
    local request_method = ngx.req.get_method()  
    local request_uri = ngx.var.uri  
    local request_args = ngx.req.get_uri_args()  
    local request_headers = ngx.req.get_headers()
    -- Forward the request to the target server  
    local res = ngx.location.capture("/proxy" .. request_uri, {  
        method = ngx.HTTP_POST,  
        args = request_args,
        always_forward_body = true,
        copy_all_vars = true, -- Copy all variables, including headers  
        ctx = { headers = request_headers } -- Store original request headers in context 
    })  

    -- Return the response from the target server  

    for k, v in pairs(res.header) do  
        if k == "x-recording-id" then
            g_recordingId = v
            g_recordingMode = mode
        end
        ngx.header[k] = v  
    end  

    ngx.status = res.status  
    ngx.print(res.body)  
    
    ngx.log(ngx.ERR, "RECORDING ID: " .. g_recordingId  .. " MODE: " .. g_recordingMode .. " mode: " .. mode)  
end
 
local function stop()
    local request_method = ngx.req.get_method()  
    local request_uri = ngx.var.uri  
    local request_args = ngx.req.get_uri_args()  
    local request_headers = ngx.req.get_headers()

    -- Forward the request to the target server  
    local res = ngx.location.capture("/proxy" .. request_uri, {  
        method = ngx.HTTP_POST,  
        args = request_args,
        always_forward_body = true,
        copy_all_vars = true, -- Copy all variables, including headers  
        ctx = { headers = request_headers } -- Store original request headers in context 
    })  

    -- Return the response from the target server  

    for k, v in pairs(res.header) do  
        ngx.header[k] = v  
    end  

    ngx.status = res.status  
    ngx.print(res.body) 
end

return {
    start = start,
    stop = stop
}