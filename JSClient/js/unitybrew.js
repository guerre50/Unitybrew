// when page loads call spacebrew setup function 
$(window).on("load", setupSpacebrew);

// wher the jquery mobile is ready to initialize the UI call the setUI function 
$(window).on("ready", setupUI);

// Spacebrew Object
var sb
	, app_name = "unitybrew test JS"
	, hueAddress = window.getQueryString('hue') || "http://192.168.0.100"
	, unityColor = {
		h: "0.5",
		s: "0.5",
		b: "0.5"
	};

/**
 * setupSpacebrew Function that creates and configures the connection to the Spacebrew server.
 * 				  It is called when the page loads.
 */
function setupSpacebrew (){
	console.log("Setting up spacebrew connection");
	sb = new Spacebrew.Client();

	sb.name(app_name);
	sb.description("Set of widgets to test unitybrew.");

	// configure the publication and subscription feeds
	sb.addPublish("text", "string");
	sb.addPublish("force", "range");
	sb.addPublish("buttonPress", "boolean");
	sb.addPublish("color", "custom");

	// override Spacebrew events - this is how you catch events coming from Spacebrew
	sb.onRangeMessage = onRangeMessage;
	sb.onOpen = onOpen;

	// connect to spacbrew
	sb.connect();
};

/**
 * Function that is called when Spacebrew connection is established
 */
function onOpen() {
	var message = "Connected as <strong>" + sb.name() + "</strong>. ";
	if (sb.name() === app_name) {
		message += "<br>You can customize this app's name in the query string by adding <strong>name=your_app_name</strong>."
	}
	$("#name").html( message );
}

/**
 * setupUI Function that create the event listeners for the sliders. It creates an callback
 * 		   function that sends a spacebrew message whenever an slide event is received.
 */
function setupUI() {
	// listen to the mouse 
	$("#buttonMsg").on("mousedown", onButtonPress);
	$("#buttonMsg").on("mouseup", onButtonRelease);
	$("#unityText").on("keyup", onTextChange);
	unityColor = {
		h: $("#h").val()/360.0,
		s: $("#s").val()/100.0,
		b: $("#a").val()/1.
	};
}

function onTextChange() {
	console.log("[onTextChanged] text has changed");
	sb.send("text", "string", $(this).val());
}

hueSliderHack.onSaturationChange(function (sat) {
	onChangeColor("s", sat/100.0);
});

hueSliderHack.onHueChange(function(hue) {
	onChangeColor("h", hue/360.0);
});

hueSliderHack.onBrightnessChange(function(bri) {
	onChangeColor("b", bri);
});

function onChangeColor(coord, value) {
	unityColor[coord] = value+"";
	if (sb && sb.isConnected) sb.send("color", "custom", JSON.stringify(unityColor));
}

hueSliderHack.onForceChange(function (force) {
	if (sb && sb.isConnected) sb.send("force", "range", parseInt(force/10.0));
});

function onRangeMessage(name, value) {
	
};

function onButtonPress (evt){
	console.log("[onButtonPress] button has been pressed"); 
	sb.send("buttonPress", "boolean", "true");
}

function onButtonRelease (evt){
	console.log("[onButtonRelease] button has been released"); 
	sb.send("buttonPress", "boolean", "false");
}

function onBooleanMessage( name, value ){
	console.log("[onBooleanMessage] boolean message received ", value);
	if (value) {
		document.body.style.background = "rgb(100,255,100)"; 
	} else {
		document.body.style.background = "rgb(220,220,220)"; 				
	}
}