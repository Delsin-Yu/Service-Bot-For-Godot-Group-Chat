from http.server import HTTPServer, BaseHTTPRequestHandler
from json import loads, dumps, JSONEncoder
from numpy import ndarray
from model import embedding

host = ('localhost', 8888)

class ResquestHandler(BaseHTTPRequestHandler):
    def do_POST(self):
        path : str = self.path
        if path != "/embedding":
            self.send_error(404)
            return
        
        content_length : int = int(self.headers['Content-Length'])
        body = self.rfile.read(content_length)

        jsonDoc = loads(body)

        if "sentence" not in jsonDoc:
            self.send_error(404)
            return

        sentence : str = jsonDoc["sentence"]
        array : ndarray = embedding(sentence)

        self.send_response(200)
        self.send_header('Content-type', 'application/json')
        self.end_headers()
        self.wfile.write(bytearray(dumps(array.tolist()), "utf-8"))

if __name__ == '__main__':
    server = HTTPServer(host, ResquestHandler)
    print("Starting server, listen at: %s:%s" % host)
    server.serve_forever()