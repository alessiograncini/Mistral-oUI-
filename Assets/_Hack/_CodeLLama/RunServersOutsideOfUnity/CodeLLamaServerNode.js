// File: CodeLlamaServerNode.js

const express = require('express');
const axios = require('axios');

const app = express();
const port = 3001;

app.use(express.json());

app.post('/api/code', async (req, res) => {
  const userMessage = req.body.message;

  try {
    const response = await axios.post('http://localhost:5000/api/code', {
      message: userMessage,
    });

    res.json(response.data);
  } catch (error) {
    console.error('Error communicating with the Python server:', error);
    res.status(500).send('Internal Server Error');
  }
});

app.listen(port, () => {
  console.log(`Server listening on port ${port}`);
});

