function changeColor() {
    var body = document.getElementById("body");
    var content = document.getElementById("content");
    var button = document.getElementById("button_change_color");

    var curr = body.getAttribute("class");

    if (curr === "bodyday") {
        body.setAttribute("class", "bodynight");
        content.setAttribute("class", "contentnight");
        button.innerHTML = "night";
    } else {
        body.setAttribute("class", "bodyday");
        content.setAttribute("class", "contentday");
        button.innerHTML = "day";
    }
}