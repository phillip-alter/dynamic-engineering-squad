//this gives jest a fake browser environment to test JS

module.exports = {
    testEnvironment: "jsdom",

    // 👇 THIS loads your setup automatically
    setupFilesAfterEnv: ["<rootDir>/jest.setup.js"],
};