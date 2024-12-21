
Console.WriteLine("ai_bench!");

var ollamaBenchmark = new OllamaBenchmark("http://quorra.homelan.binaryward.com:11434");
var models = await ollamaBenchmark.GetModels();

var modelIgnoreList = new string[]{
    "nomic-embed-text:137m-v1.5-fp16", // does not support chat - embedding model
    "mxbai-embed-large:335m-v1-fp16", // embedding model
    "snowflake-arctic-embed:335m-l-fp16",
    "zw66/llama3-chat-8.0bpw:latest",
    "unclemusclez/jina-embeddings-v2-base-code:f16",
    "jina/jina-embeddings-v2-base-en:latest",
    "bge-large:335m-en-v1.5-fp16",
    "all-minilm:33m-l12-v2-fp16",
};

models = models.Where(x => !modelIgnoreList.Contains(x)).ToArray();

var prompts = new List<string> {
    "How to make a sandwich",
    "How to make a sandwich with a twist",
    "What are the benefits of exercise?",
    "Translate the following English text to French: 'The quick brown fox jumps over the lazy dog.'",
    "Generate a summary of the book 'To Kill a Mockingbird' by Harper Lee.",
    "Given a list of numbers [1, 2, 3, 4, 5], calculate the average.",
    // "Create a function that sorts an array of strings in alphabetical order.",
    // "Find the shortest path between two nodes in a graph using Dijkstra's algorithm.",
    // "Design a database schema for an online shopping system.",
    // "Explain the concept of quantum entanglement",
    // "How does the human brain process emotions?",
    // "Describe the process of photosynthesis in plants",
    // "Discuss the impact of climate change on biodiversity",
    // "Explain the theory of relativity",
    // "What are the ethical implications of artificial intelligence?",
    // "How to bake a chocolate cake",
    "What is the capital of France?",
    // "How to tie a necktie",
    
//     @"Develop a budgeting tool that helps users manage their monthly expenses. Your tool should: 
// 1. Categorize expenses into essentials, savings, and discretionary spending.
// 2. Allow users to input their monthly income and distribute it across these categories.
// 3. Suggest percentages for each category based on best practices.
// 4. Provide a simple interface for tracking and adjusting expenses throughout the month.
// 5. Generate a summary report of spending vs. income at the end of each month.",

//     @"As an event planning assistant, your task is to organize a detailed schedule for a three-day corporate retreat. You must: 
// 1. Identify suitable activities based on the group's interest in team-building and professional development. 
// 2. Allocate time slots for each activity, considering optimal participation times. 
// 3. Ensure there are ample breaks for meals and relaxation. 
// 4. Follow guidelines to accommodate dietary restrictions in meal planning. 
// 5. Present the schedule in a clear, hour-by-hour format.",

// // Code Generation
// "Write a Python script that reads a CSV file containing timestamps and temperatures, calculates the daily average temperature, and saves the result to a new CSV file. Include comments explaining your code.",
// "Generate a JavaScript code snippet for an interactive web page element that displays a dropdown menu when clicked. The dropdown should list three options: 'Home', 'About', and 'Contact'. Include comments on how to integrate it with HTML and CSS.",
// "Create a Java class named 'Book' with private attributes for title, author, and ISBN. Write getter and setter methods for each attribute and a method to display book details. Include a main method to demonstrate creating and displaying a Book instance.",
// "Write a C++ program that demonstrates dynamic memory allocation and deallocation using pointers. Create a class named 'ArrayHandler' with methods to allocate an array dynamically, fill it with numbers, and then deallocate the memory properly.",
// "Provide an SQL query that selects the name and email of users from a 'Users' table where the user's account is more than one year old and has 'premium' status. The table includes columns for name, email, account_creation_date, and status.",
// "Write a Python script using scikit-learn to train a linear regression model on a dataset provided in a CSV file. The dataset contains columns for 'hours_studied' and 'test_score'. Output the model's accuracy on a test dataset.",
// "Design a simple Kotlin function for an Android app that takes a user's input string, reverses it, and displays a Toast message with the reversed string. Include comments explaining the function.",
// "Create an HTML page with a CSS stylesheet that designs a responsive profile card. The card should contain an image, name, and a short bio. Use media queries to ensure it adjusts for desktop and mobile views.",
// "Write a bash script that searches for all JPEG files in a directory and its subdirectories, renames them by adding the current date as a prefix, and moves them to a specified 'Archived' directory.",
// "Write a Dockerfile that creates an image for deploying a simple Python web application. The application uses Flask and listens on port 5000. Include comments explaining each step in the Dockerfile.",

// // Programming Assistant
// "Explain what the following Python code does: `list(filter(lambda x: x % 2 == 0, range(10)))`.",
// "I'm getting a 'NullPointerException' in my Java application when trying to access an object's method. What are the common causes, and how can I fix it?",
// "Can you suggest best practices for managing memory in C++ applications?",
// "Here's a JavaScript function I wrote that adds numbers in an array. How can I refactor this for better performance and readability? `function addNumbers(arr) { let sum = 0; for(let i = 0; i < arr.length; i++) { sum += arr[i]; } return sum; }`",
// "Explain the quicksort algorithm and provide an implementation in Python.",
// "How do I use the Pandas library in Python to read a CSV file and filter rows based on column values?",
// "What are some security best practices I should follow when developing a web application to prevent SQL injection attacks?",
// "My Python script for processing large datasets is running very slowly. What are some strategies I can use to optimize its performance?",
// "Can you compare how inheritance works in Java versus Python?",
// "I'm designing a new feature for our application that requires dynamically changing its behavior based on user input. Which design pattern would you recommend and why?",

    };


// Randomly sort models
var random = new Random();
models = models.OrderBy(x => random.Next()).ToArray();

var results = await ollamaBenchmark.RunAsync(models, prompts);

foreach (var model in results)
{
    Console.WriteLine($"Model, Duration, Prompt");
    foreach (var prompt in model.Value)
    {
        Console.WriteLine($"{model.Key}, {prompt.Value}, {prompt.Key}");
    }
}
