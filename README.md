This will have NATS Jetstream configuration in a json file

Json file is parsed and configuration is read to create NATS streams and subjects

This must be integrated to Jenkins deployment pipeline and executed during deployment

To run NATS server locally :
nats-server -js -http_port 8080
