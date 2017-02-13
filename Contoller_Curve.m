a = 1; %Sensitivity
x = -1:.05:1; %Input
v = 100; %max velocity

relativeSpeed = (exp(a.*x)-1)./(exp(a.*x)+1);
relativeSpeed2 = .5*x.^3;

plot(x, relativeSpeed);
hold on;
plot(x, relativeSpeed2);
grid on;

driveSpeed = v .* relativeSpeed;