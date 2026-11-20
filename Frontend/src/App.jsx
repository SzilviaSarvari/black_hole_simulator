import './App.css'

function App() {
    const StartSimulation = async () => {
        await fetch("http://localhost:5000/start", {
            method: 'POST',
        });
    };
    return (
        <div className="container">
            <img src="/blackhole.svg" alt="Black hole"></img>
            <button onClick={StartSimulation}>START SIMULATION</button>
        </div>
    );
}

export default App
