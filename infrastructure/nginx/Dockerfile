FROM nginx:alpine

RUN apk add --no-cache openssl bash

COPY scripts/generate-certs.sh /generate-certs.sh
COPY infrastructure/nginx/docker-entrypoint.sh /docker-entrypoint.sh
COPY infrastructure/nginx/nginx.conf /etc/nginx/conf.d/team-hub.conf
COPY infrastructure/nginx/http_buffers.conf /etc/nginx/conf.d/00-buffers.conf
COPY infrastructure/nginx/snippets/ /etc/nginx/snippets/team-hub/
RUN rm -f /etc/nginx/conf.d/default.conf && chmod +x /generate-certs.sh /docker-entrypoint.sh

EXPOSE 80 443 8081
ENTRYPOINT ["/docker-entrypoint.sh"]
