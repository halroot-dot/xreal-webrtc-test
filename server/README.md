### Usage

```bash
$ cd server
$ npm install
$ npm start
```

### For EC2

```bash
sudo yum update -y
sudo yum install nodejs npm -y
sudo npm install n -g

sudo yum install git -y
git clone https://github.com/halroot-dot/xreal-webrtc-test.git
cd xreal-webrtc-test/server

npm install
npm start

// 起動を継続する
sudo npm install pm2 -g
pm2 start main.js
```
