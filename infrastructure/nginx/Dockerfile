FROM nginx:alpine

RUN apk add --no-cache openssl bash

COPY scripts/generate-certs.sh /generate-certs.sh
COPY infrastructure/nginx/docker-entrypoint.sh /docker-entrypoint.sh
COPY infrastructure/nginx/nginx.conf /etc/nginx/conf.d/team-hub.conf
RUN rm -f /etc/nginx/conf.d/default.conf && chmod +x /generate-certs.sh /docker-entrypoint.sh

EXPOSE 80 443
ENTRYPOINT ["/docker-entrypoint.sh"]
