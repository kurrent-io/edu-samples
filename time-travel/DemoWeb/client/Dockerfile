FROM node:22

COPY . /workspace/DemoWeb

WORKDIR ./workspace/DemoWeb

RUN npm i
RUN npm run build

CMD ["npm", "run", "start"]
