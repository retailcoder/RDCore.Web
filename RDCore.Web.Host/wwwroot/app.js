// calls the endpoint the form names with the name typed, and shows the status and the JSON it answers: the value, or a problem details body.
const form = document.getElementById("hello");
const answer = document.getElementById("answer");

form.addEventListener("submit", async (event) => {
  event.preventDefault();
  const name = document.getElementById("name").value;
  const response = await fetch(form.dataset.endpoint + encodeURIComponent(name), { headers: { Accept: "application/json" } });
  const text = await response.text();
  let body = text;
  try {
    body = JSON.stringify(JSON.parse(text), null, 2);
  } catch {
    // not JSON: shown as it is.
  }
  answer.textContent = `${response.status} ${response.statusText}\n${body}`;
});
