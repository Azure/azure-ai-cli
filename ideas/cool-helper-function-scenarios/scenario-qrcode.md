Make a helper function from the following info:

You can use the QRtag.net API as a normal image. Just use the following URL structure:

https://qrtag.net/api/qr(_transparent)(_[size]).[png|svg](?url=[URL])
Your QR code will link by default with the page on which is was embedded, in order to do so, it will use the referrer that what send in the request. If you want to use another URL or don't want to rely on the referrer (since it will not always work) add the "?url=[URL]" parameter, where [URL] is the URL that the QRtag links. For the image output you can choose between PNG (a transparent bitmap image) or SVG (a vector orientated image). The size of each pixel can be determined with the [size] parameter.

